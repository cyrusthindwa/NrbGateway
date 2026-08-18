using CHL.NrbGateway.Application.Common.Interfaces;
using CHL.NrbGateway.Application.DTOs;
using CHL.NrbGateway.Domain.Entities.Config;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CHL.NrbGateway.Api.Portal.Controllers;

[ApiController]
[Route("api/v1/portal/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class CompaniesController : ControllerBase
{
    private readonly IConfigDbContext _configDbContext;

    public CompaniesController(IConfigDbContext configDbContext)
    {
        _configDbContext = configDbContext;
    }

    [HttpGet]
    public ActionResult<IEnumerable<CompanyDto>> GetCompanies()
    {
        var list = _configDbContext.Companies
            .OrderBy(c => c.Name)
            .Select(c => new CompanyDto(c.Id, c.Name, c.ShortCode, c.CreatedAt))
            .ToList();

        return Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult<CompanyDto>> CreateCompany([FromBody] CreateCompanyDto dto, CancellationToken cancellationToken)
    {
        var exists = _configDbContext.Companies
            .Any(c => c.ShortCode.ToLower() == dto.ShortCode.ToLower());

        if (exists)
            return BadRequest(new { message = $"Company with short code '{dto.ShortCode}' already exists." });

        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            ShortCode = dto.ShortCode.ToUpperInvariant(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _configDbContext.Add(company);
        await _configDbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetCompanies), new CompanyDto(company.Id, company.Name, company.ShortCode, company.CreatedAt));
    }
}

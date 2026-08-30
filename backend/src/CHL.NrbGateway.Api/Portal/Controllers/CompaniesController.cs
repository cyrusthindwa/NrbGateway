using System.Security.Claims;
using CHL.NrbGateway.Application.Common.Interfaces;
using CHL.NrbGateway.Application.DTOs;
using CHL.NrbGateway.Domain.Entities.Config;
using CHL.NrbGateway.Domain.Enums;
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
            Name = dto.Name.Trim(),
            ShortCode = dto.ShortCode.ToUpperInvariant(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _configDbContext.Add(company);
        AddAuditLog(SettingArea.COMPANY, $"company.{company.ShortCode}.created", null, company.Name);
        await _configDbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetCompanies), new CompanyDto(company.Id, company.Name, company.ShortCode, company.CreatedAt));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CompanyDto>> UpdateCompany(Guid id, [FromBody] UpdateCompanyDto dto, CancellationToken cancellationToken)
    {
        var company = _configDbContext.Companies.FirstOrDefault(c => c.Id == id);
        if (company == null) return NotFound(new { message = "Company not found." });

        var shortCode = dto.ShortCode.ToUpperInvariant();
        if (_configDbContext.Companies.Any(c => c.Id != id && c.ShortCode.ToLower() == shortCode.ToLower()))
            return BadRequest(new { message = $"Another company already uses short code '{shortCode}'." });

        var oldName = company.Name;
        var oldCode = company.ShortCode;

        company.Name = dto.Name.Trim();
        company.ShortCode = shortCode;
        _configDbContext.Update(company);

        AddAuditLog(SettingArea.COMPANY, $"company.{id}.updated", $"{oldName} ({oldCode})", $"{company.Name} ({company.ShortCode})");
        await _configDbContext.SaveChangesAsync(cancellationToken);

        return Ok(new CompanyDto(company.Id, company.Name, company.ShortCode, company.CreatedAt));
    }

    private void AddAuditLog(SettingArea area, string key, string? oldValue, string newValue)
    {
        _configDbContext.Add(new ConfigAuditLog
        {
            Id = Guid.NewGuid(),
            AdminId = CurrentAdminId(),
            SettingArea = area,
            SettingKey = key,
            OldValue = oldValue,
            NewValue = newValue,
            ChangedAt = DateTimeOffset.UtcNow
        });
    }

    private Guid CurrentAdminId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (Guid.TryParse(claim, out var id) && _configDbContext.AdminUsers.Any(a => a.Id == id))
            return id;

        return _configDbContext.AdminUsers.OrderBy(a => a.CreatedAt).Select(a => a.Id).FirstOrDefault();
    }
}

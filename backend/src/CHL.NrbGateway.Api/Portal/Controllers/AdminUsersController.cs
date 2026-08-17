using CHL.NrbGateway.Application.Common.Interfaces;
using CHL.NrbGateway.Application.DTOs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CHL.NrbGateway.Api.Portal.Controllers;

[ApiController]
[Route("api/v1/portal/admin-users")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class AdminUsersController : ControllerBase
{
    private readonly IConfigDbContext _configDbContext;

    public AdminUsersController(IConfigDbContext configDbContext)
    {
        _configDbContext = configDbContext;
    }

    [HttpGet]
    public ActionResult<PaginatedResponseDto<AdminUserDto>> GetAdminUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = _configDbContext.AdminUsers.AsQueryable();
        var total = query.Count();

        var data = query
            .OrderBy(a => a.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AdminUserDto(
                a.Id,
                a.Name,
                a.Email,
                a.Status.ToString(),
                a.CreatedAt
            ))
            .ToList();

        int totalPages = (int)Math.Ceiling((double)total / pageSize);
        if (totalPages < 1) totalPages = 1;

        return Ok(new PaginatedResponseDto<AdminUserDto>(
            Data: data,
            Total: total,
            Page: page,
            PageSize: pageSize,
            TotalPages: totalPages
        ));
    }
}

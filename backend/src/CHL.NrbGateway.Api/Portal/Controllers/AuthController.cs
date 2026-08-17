using CHL.NrbGateway.Application.Common.Interfaces;
using CHL.NrbGateway.Application.DTOs;
using CHL.NrbGateway.Domain.Entities.Config;
using CHL.NrbGateway.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CHL.NrbGateway.Api.Portal.Controllers;

[ApiController]
[Route("api/v1/portal/[controller]")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IConfigDbContext _configDbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IConfigDbContext configDbContext,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        ILogger<AuthController> logger)
    {
        _configDbContext = configDbContext;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AdminLoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AdminLoginResponseDto>> Login(
        [FromBody] AdminLoginDto request,
        CancellationToken cancellationToken)
    {
        // Seed default admin if table is empty for development ease
        if (!_configDbContext.AdminUsers.Any())
        {
            var seedAdmin = new AdminUser
            {
                Id = Guid.NewGuid(),
                Name = "CHL ICT Administrator",
                Email = "admin@continental.mw",
                PasswordHash = _passwordHasher.HashPassword("Admin123!"),
                Status = AdminStatus.ACTIVE,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _configDbContext.Add(seedAdmin);
            await _configDbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded initial ICT Admin User: admin@continental.mw");
        }

        var admin = _configDbContext.AdminUsers
            .FirstOrDefault(a => a.Email.ToLower() == request.Email.ToLower());

        if (admin == null || admin.Status != AdminStatus.ACTIVE || !_passwordHasher.VerifyPassword(request.Password, admin.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid admin credentials or account disabled." });
        }

        admin.LastLoginAt = DateTimeOffset.UtcNow;
        await _configDbContext.SaveChangesAsync(cancellationToken);

        var token = _jwtTokenService.GenerateToken(admin);

        return Ok(new AdminLoginResponseDto(
            Token: token,
            AdminId: admin.Id,
            Name: admin.Name,
            Email: admin.Email
        ));
    }
}

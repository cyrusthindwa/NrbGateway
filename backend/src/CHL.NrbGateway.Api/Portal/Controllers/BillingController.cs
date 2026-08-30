using System.Security.Claims;
using CHL.NrbGateway.Application.Common.Interfaces;
using CHL.NrbGateway.Application.DTOs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CHL.NrbGateway.Api.Portal.Controllers;

[ApiController]
[Route("api/v1/portal/billing")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class BillingController : ControllerBase
{
    private readonly IConfigDbContext _configDbContext;
    private readonly IBillingService _billingService;

    public BillingController(IConfigDbContext configDbContext, IBillingService billingService)
    {
        _configDbContext = configDbContext;
        _billingService = billingService;
    }

    [HttpGet("today")]
    public async Task<ActionResult<IReadOnlyList<BillingTodayDto>>> GetToday(CancellationToken cancellationToken)
    {
        return Ok(await _billingService.GetTodayUsageAsync(cancellationToken));
    }

    [HttpGet("monthly-reports")]
    public ActionResult<IEnumerable<MonthlyUsageReportDto>> GetMonthlyReports(
        [FromQuery] int? year = null,
        [FromQuery] int? month = null)
    {
        var query = _configDbContext.MonthlyUsageReports.AsQueryable();
        if (year.HasValue) query = query.Where(m => m.PeriodYear == year.Value);
        if (month.HasValue) query = query.Where(m => m.PeriodMonth == month.Value);

        var projects = _configDbContext.Projects.ToList();
        var companies = _configDbContext.Companies.ToList();

        var data = query
            .OrderByDescending(m => m.PeriodYear)
            .ThenByDescending(m => m.PeriodMonth)
            .Take(200)
            .ToList()
            .Select(m =>
            {
                var project = projects.FirstOrDefault(p => p.Id == m.ProjectId);
                var company = project != null ? companies.FirstOrDefault(c => c.Id == project.CompanyId) : null;
                return new MonthlyUsageReportDto(
                    m.Id, m.ProjectId, project?.Name ?? "Unknown", project?.ShortCode ?? "?",
                    m.CompanyId, company?.Name ?? "Unknown",
                    m.PeriodYear, m.PeriodMonth, m.RequestCount, m.TotalCost, m.GeneratedAt);
            })
            .ToList();

        return Ok(data);
    }

    [HttpPost("monthly-reports/generate")]
    public async Task<IActionResult> GenerateReports([FromBody] GenerateReportsDto dto, CancellationToken cancellationToken)
    {
        await _billingService.GenerateMonthlyReportsAsync(dto.PeriodYear, dto.PeriodMonth, cancellationToken);
        return Ok(new { message = "Monthly usage reports generated." });
    }

    [HttpGet("invoices")]
    public ActionResult<IEnumerable<BillingInvoiceDto>> GetInvoices()
    {
        var companies = _configDbContext.Companies.ToList();

        var data = _configDbContext.BillingInvoices
            .OrderByDescending(i => i.GeneratedAt)
            .Take(200)
            .ToList()
            .Select(i =>
            {
                var company = companies.FirstOrDefault(c => c.Id == i.CompanyId);
                return new BillingInvoiceDto(
                    i.Id, i.CompanyId, company?.Name ?? "Unknown", company?.ShortCode ?? "?",
                    i.PeriodStart, i.PeriodEnd, i.TotalAmount, i.Status, i.GeneratedAt, i.PaidAt);
            })
            .ToList();

        return Ok(data);
    }

    [HttpPost("invoices/generate")]
    public async Task<ActionResult<BillingInvoiceDto>> GenerateInvoice(
        [FromBody] GenerateInvoiceDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var invoice = await _billingService.GenerateInvoiceAsync(
                dto.CompanyId, dto.PeriodYear, dto.PeriodMonth, CurrentAdminId(), cancellationToken);
            return Ok(invoice);
        }
        catch (InvalidOperationException)
        {
            return BadRequest(new { message = "Company not found." });
        }
    }

    private Guid CurrentAdminId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (Guid.TryParse(claim, out var id) && _configDbContext.AdminUsers.Any(a => a.Id == id))
            return id;

        return _configDbContext.AdminUsers.OrderBy(a => a.CreatedAt).Select(a => a.Id).FirstOrDefault();
    }
}

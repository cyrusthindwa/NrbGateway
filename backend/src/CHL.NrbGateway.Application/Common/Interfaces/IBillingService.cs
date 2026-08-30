using CHL.NrbGateway.Application.DTOs;

namespace CHL.NrbGateway.Application.Common.Interfaces;

public interface IBillingService
{
    Task<IReadOnlyList<BillingTodayDto>> GetTodayUsageAsync(CancellationToken cancellationToken = default);
    Task GenerateMonthlyReportsAsync(int year, int month, CancellationToken cancellationToken = default);
    Task<BillingInvoiceDto> GenerateInvoiceAsync(Guid companyId, int year, int month, Guid adminId, CancellationToken cancellationToken = default);
}

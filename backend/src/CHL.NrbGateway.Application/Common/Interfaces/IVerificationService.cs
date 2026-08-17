using CHL.NrbGateway.Application.DTOs;

namespace CHL.NrbGateway.Application.Common.Interfaces;

public interface IVerificationService
{
    Task<IntermediateVerificationResultDto> VerifyIntermediateAsync(
        Guid subsidiaryId,
        string subsidiaryShortCode,
        IntermediateVerificationRequestDto request,
        CancellationToken cancellationToken = default
    );

    Task<BasicVerificationResultDto> VerifyBasicAsync(
        Guid subsidiaryId,
        string subsidiaryShortCode,
        BasicVerificationRequestDto request,
        CancellationToken cancellationToken = default
    );

    Task<TextLookupResultDto> TextLookupAsync(
        Guid subsidiaryId,
        string subsidiaryShortCode,
        TextLookupRequestDto request,
        CancellationToken cancellationToken = default
    );

    Task<AdvancedVerificationResultDto> VerifyAdvancedAsync(
        Guid subsidiaryId,
        string subsidiaryShortCode,
        AdvancedVerificationRequestDto request,
        CancellationToken cancellationToken = default
    );

    Task<Models.RevalidationResultDto> RevalidateAllAsync(
        Guid adminId,
        CancellationToken cancellationToken = default
    );
}

using CHL.NrbGateway.Application.DTOs;

namespace CHL.NrbGateway.Application.Common.Interfaces;

public interface IVerificationService
{
    Task<IntermediateVerificationResultDto> VerifyIntermediateAsync(
        Guid projectId,
        string projectCode,
        IntermediateVerificationRequestDto request,
        CancellationToken cancellationToken = default
    );

    Task<BasicVerificationResultDto> VerifyBasicAsync(
        Guid projectId,
        string projectCode,
        BasicVerificationRequestDto request,
        CancellationToken cancellationToken = default
    );

    Task<TextLookupResultDto> TextLookupAsync(
        Guid projectId,
        string projectCode,
        TextLookupRequestDto request,
        CancellationToken cancellationToken = default
    );

    Task<AdvancedVerificationResultDto> VerifyAdvancedAsync(
        Guid projectId,
        string projectCode,
        AdvancedVerificationRequestDto request,
        CancellationToken cancellationToken = default
    );

    Task<Models.RevalidationResultDto> RevalidateAllAsync(
        Guid adminId,
        CancellationToken cancellationToken = default
    );
}

using CHL.NrbGateway.Application.Models;
using CHL.NrbGateway.Domain.Enums;

namespace CHL.NrbGateway.Application.Common.Interfaces;

public interface INrbTierAdapter
{
    NrbTier Tier { get; }
    
    Task<NrbIntermediateResponseModel> VerifyIntermediateAsync(NrbIntermediateRequestModel request, CancellationToken cancellationToken = default);
    Task<NrbBasicResponseModel> VerifyBasicAsync(NrbBasicRequestModel request, CancellationToken cancellationToken = default);
    Task<NrbTextLookupResponseModel> TextLookupAsync(NrbTextLookupRequestModel request, CancellationToken cancellationToken = default);
    Task<NrbAdvancedResponseModel> VerifyAdvancedAsync(NrbAdvancedRequestModel request, CancellationToken cancellationToken = default);
}

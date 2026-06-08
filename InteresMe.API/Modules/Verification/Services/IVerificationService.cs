using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.Modules.Verification.DTOs;

namespace InteresMe.API.Modules.Verification.Services;

public interface IVerificationService
{
    Task<ApplicationResult<VerificationSummaryResponse>> GetMyVerificationAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<ApplicationResult<SendEmailVerificationResponse>> SendEmailVerificationAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<ApplicationResult<VerificationSummaryResponse>> ConfirmEmailVerificationAsync(
        Guid userId,
        ConfirmEmailVerificationRequest request,
        CancellationToken cancellationToken = default);
}

using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.Modules.Verification.DTOs.Common;
using InteresMe.API.Modules.Verification.DTOs.Email;
using InteresMe.API.Modules.Verification.DTOs.Phone;

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
        ConfirmEmailVerificationRequest request,
        CancellationToken cancellationToken = default);

    Task<ApplicationResult<SendPhoneVerificationResponse>> SendPhoneVerificationAsync(
        Guid userId,
        SendPhoneVerificationRequest request,
        CancellationToken cancellationToken = default);

    Task<ApplicationResult<VerificationSummaryResponse>> ConfirmPhoneVerificationAsync(
        Guid userId,
        ConfirmPhoneVerificationRequest request,
        CancellationToken cancellationToken = default);
}

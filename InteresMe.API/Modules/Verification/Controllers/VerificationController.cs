using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.BuildingBlocks.Security;
using InteresMe.API.Modules.Verification.DTOs.Email;
using InteresMe.API.Modules.Verification.DTOs.Phone;
using InteresMe.API.Modules.Verification.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static InteresMe.API.BuildingBlocks.Results.ApplicationResultMapper;

namespace InteresMe.API.Modules.Verification.Controllers;

[ApiController]
[Authorize]
[Route("api/verification")]
public class VerificationController(
    ICurrentUser currentUser,
    IVerificationService verificationService) : ControllerBase
{
    [HttpGet("me")]
    public Task<IActionResult> GetMyVerification(
        CancellationToken cancellationToken) =>
        WithCurrentUserId(
            userId => verificationService.GetMyVerificationAsync(userId, cancellationToken));

    [HttpPost("email/send")]
    public Task<IActionResult> SendEmailVerification(
        CancellationToken cancellationToken) =>
        WithCurrentUserId(
            userId => verificationService.SendEmailVerificationAsync(userId, cancellationToken));

    [HttpPost("email/confirm")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmailVerification(
        [FromBody] ConfirmEmailVerificationRequest request,
        CancellationToken cancellationToken) =>
        ToActionResult(await verificationService.ConfirmEmailVerificationAsync(
            request,
            cancellationToken));

    [HttpPost("phone/send")]
    public Task<IActionResult> SendPhoneVerification(
        [FromBody] SendPhoneVerificationRequest request,
        CancellationToken cancellationToken) =>
        WithCurrentUserId(
            userId => verificationService.SendPhoneVerificationAsync(
                userId,
                request,
                cancellationToken));

    [HttpPost("phone/confirm")]
    public Task<IActionResult> ConfirmPhoneVerification(
        [FromBody] ConfirmPhoneVerificationRequest request,
        CancellationToken cancellationToken) =>
        WithCurrentUserId(
            userId => verificationService.ConfirmPhoneVerificationAsync(
                userId,
                request,
                cancellationToken));

    private async Task<IActionResult> WithCurrentUserId<T>(
        Func<Guid, Task<ApplicationResult<T>>> action)
    {
        Guid userId;

        try
        {
            userId = currentUser.UserId;
        }
        catch (InvalidOperationException)
        {
            return Unauthorized(new { message = "Invalid access token." });
        }

        return ToActionResult(await action(userId));
    }
}

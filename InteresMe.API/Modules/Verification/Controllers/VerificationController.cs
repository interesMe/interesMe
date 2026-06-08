using InteresMe.API.BuildingBlocks.Email;
using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.BuildingBlocks.Security;
using InteresMe.API.Modules.Verification.DTOs;
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
    IVerificationService verificationService,
    IEmailService emailService,
    IWebHostEnvironment webHostEnvironment,
    ILogger<VerificationController> logger) : ControllerBase
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
    public Task<IActionResult> ConfirmEmailVerification(
        [FromBody] ConfirmEmailVerificationRequest request,
        CancellationToken cancellationToken) =>
        WithCurrentUserId(
            userId => verificationService.ConfirmEmailVerificationAsync(
                userId,
                request,
                cancellationToken));

    [HttpPost("test-email")]
    [AllowAnonymous]
    public async Task<IActionResult> SendTestEmail(
        [FromBody] TestEmailRequest request,
        CancellationToken cancellationToken)
    {
        if (!webHostEnvironment.IsDevelopment())
        {
            return NotFound();
        }

        var email = request.Email?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new { message = "Email is required." });
        }

        try
        {
            await emailService.SendWelcomeEmailAsync(
                email,
                "Max",
                cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Failed to send development test email to {Email}.",
                email);

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { message = "Failed to send test email." });
        }

        return Ok(new { message = "Test email sent." });
    }

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

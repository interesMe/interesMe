using System.Data;
using InteresMe.API.BuildingBlocks.Email;
using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.Data;
using InteresMe.API.Modules.Verification.DTOs.Common;
using InteresMe.API.Modules.Verification.DTOs.Email;
using InteresMe.API.Modules.Verification.DTOs.Phone;
using InteresMe.API.Modules.Verification.Models;
using Microsoft.EntityFrameworkCore;

namespace InteresMe.API.Modules.Verification.Services;

public sealed class VerificationService(
    AppDbContext dbContext,
    IEmailService emailService,
    IVerificationTokenService verificationTokenService,
    IPhoneNumberNormalizer phoneNumberNormalizer,
    IWebHostEnvironment webHostEnvironment,
    ILogger<VerificationService> logger) : IVerificationService
{
    private static readonly TimeSpan EmailVerificationTokenLifetime = TimeSpan.FromHours(24);
    private static readonly TimeSpan PhoneVerificationTokenLifetime = TimeSpan.FromMinutes(10);

    public async Task<ApplicationResult<VerificationSummaryResponse>> GetMyVerificationAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var userExists = await dbContext.Users
            .AnyAsync(user => user.Id == userId, cancellationToken);

        if (!userExists)
        {
            return ApplicationResult<VerificationSummaryResponse>.Failure(
                ApplicationErrorKind.NotFound,
                "User was not found.");
        }

        var verification = await dbContext.UserVerifications
            .FirstOrDefaultAsync(
                userVerification => userVerification.UserId == userId,
                cancellationToken);

        if (verification is null)
        {
            var now = DateTime.UtcNow;
            verification = new UserVerification
            {
                UserId = userId,
                TrustScore = 0,
                CreatedAt = now,
                UpdatedAt = now
            };

            dbContext.UserVerifications.Add(verification);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return ApplicationResult<VerificationSummaryResponse>.Success(
            ToSummaryResponse(verification));
    }

    public async Task<ApplicationResult<SendEmailVerificationResponse>> SendEmailVerificationAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                user => user.Id == userId,
                cancellationToken);

        if (user is null)
        {
            return ApplicationResult<SendEmailVerificationResponse>.Failure(
                ApplicationErrorKind.NotFound,
                "User was not found.");
        }

        var now = DateTime.UtcNow;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);

        try
        {
            var createdToken = await verificationTokenService.CreateAsync(
                userId,
                VerificationType.Email,
                user.Email,
                EmailVerificationTokenLifetime,
                now,
                cancellationToken);

            await emailService.SendEmailVerificationAsync(
                user.Email,
                user.DisplayName,
                createdToken.Token,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return ApplicationResult<SendEmailVerificationResponse>.Success(new SendEmailVerificationResponse
            {
                Token = webHostEnvironment.IsDevelopment() ? createdToken.Token : null,
                ExpiresAt = createdToken.ExpiresAt
            });
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Failed to send email verification message to user {UserId}.",
                userId);

            return ApplicationResult<SendEmailVerificationResponse>.Failure(
                ApplicationErrorKind.InternalServerError,
                "Failed to send email verification message.");
        }
    }

    public async Task<ApplicationResult<VerificationSummaryResponse>> ConfirmEmailVerificationAsync(
        ConfirmEmailVerificationRequest request,
        CancellationToken cancellationToken = default)
    {
        var token = request.Token?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(token))
        {
            return ApplicationResult<VerificationSummaryResponse>.Failure(
                ApplicationErrorKind.Validation,
                "Verification token is required.");
        }

        var now = DateTime.UtcNow;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);

        var verificationToken = await verificationTokenService.FindValidTokenAsync(
            token,
            VerificationType.Email,
            userId: null,
            now,
            cancellationToken);

        if (verificationToken is null)
        {
            return ApplicationResult<VerificationSummaryResponse>.Failure(
                ApplicationErrorKind.Unauthorized,
                "Invalid or expired verification token.");
        }

        var verification = await GetOrCreateVerificationAsync(
            verificationToken.UserId,
            now,
            cancellationToken);

        verificationToken.ConsumedAt = now;
        verification.EmailVerifiedAt = now;
        verification.UpdatedAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return ApplicationResult<VerificationSummaryResponse>.Success(
            ToSummaryResponse(verification));
    }

    public async Task<ApplicationResult<SendPhoneVerificationResponse>> SendPhoneVerificationAsync(
        Guid userId,
        SendPhoneVerificationRequest request,
        CancellationToken cancellationToken = default)
    {
        var phoneNumber = phoneNumberNormalizer.Normalize(request.PhoneNumber);

        if (phoneNumber is null)
        {
            return ApplicationResult<SendPhoneVerificationResponse>.Failure(
                ApplicationErrorKind.Validation,
                "Valid phone number is required.");
        }

        var userExists = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(
                user => user.Id == userId,
                cancellationToken);

        if (!userExists)
        {
            return ApplicationResult<SendPhoneVerificationResponse>.Failure(
                ApplicationErrorKind.NotFound,
                "User was not found.");
        }

        var now = DateTime.UtcNow;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);

        try
        {
            var createdToken = await verificationTokenService.CreateAsync(
                userId,
                VerificationType.Phone,
                phoneNumber,
                PhoneVerificationTokenLifetime,
                now,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return ApplicationResult<SendPhoneVerificationResponse>.Success(new SendPhoneVerificationResponse
            {
                Token = webHostEnvironment.IsDevelopment() ? createdToken.Token : null,
                ExpiresAt = createdToken.ExpiresAt
            });
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Failed to create phone verification token for user {UserId}.",
                userId);

            return ApplicationResult<SendPhoneVerificationResponse>.Failure(
                ApplicationErrorKind.InternalServerError,
                "Failed to create phone verification token.");
        }
    }

    public async Task<ApplicationResult<VerificationSummaryResponse>> ConfirmPhoneVerificationAsync(
        Guid userId,
        ConfirmPhoneVerificationRequest request,
        CancellationToken cancellationToken = default)
    {
        var token = request.Token?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(token))
        {
            return ApplicationResult<VerificationSummaryResponse>.Failure(
                ApplicationErrorKind.Validation,
                "Verification token is required.");
        }

        var now = DateTime.UtcNow;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);

        var verificationToken = await verificationTokenService.FindValidTokenAsync(
            token,
            VerificationType.Phone,
            userId,
            now,
            cancellationToken);

        if (verificationToken is null ||
            string.IsNullOrWhiteSpace(verificationToken.Target))
        {
            return ApplicationResult<VerificationSummaryResponse>.Failure(
                ApplicationErrorKind.Unauthorized,
                "Invalid or expired verification token.");
        }

        var verification = await GetOrCreateVerificationAsync(
            userId,
            now,
            cancellationToken);

        verificationToken.ConsumedAt = now;
        verification.PhoneNumber = verificationToken.Target;
        verification.PhoneVerifiedAt = now;
        verification.UpdatedAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return ApplicationResult<VerificationSummaryResponse>.Success(
            ToSummaryResponse(verification));
    }

    private async Task<UserVerification> GetOrCreateVerificationAsync(
        Guid userId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var verification = await dbContext.UserVerifications
            .FirstOrDefaultAsync(
                userVerification => userVerification.UserId == userId,
                cancellationToken);

        if (verification is not null)
        {
            return verification;
        }

        verification = new UserVerification
        {
            UserId = userId,
            TrustScore = 0,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.UserVerifications.Add(verification);

        return verification;
    }

    private static VerificationSummaryResponse ToSummaryResponse(
        UserVerification verification) => new()
    {
        UserId = verification.UserId,
        EmailVerified = verification.EmailVerifiedAt is not null,
        PhoneVerified = verification.PhoneVerifiedAt is not null,
        IdentityVerified = verification.IdentityVerifiedAt is not null,
        PhoneNumber = verification.PhoneNumber,
        EmailVerifiedAt = verification.EmailVerifiedAt,
        PhoneVerifiedAt = verification.PhoneVerifiedAt,
        IdentityVerifiedAt = verification.IdentityVerifiedAt,
        TrustScore = verification.TrustScore
    };

}

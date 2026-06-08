using System.Data;
using InteresMe.API.BuildingBlocks.Email;
using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.Data;
using InteresMe.API.Modules.Verification.DTOs;
using InteresMe.API.Modules.Verification.Models;
using InteresMe.API.Security;
using Microsoft.EntityFrameworkCore;

namespace InteresMe.API.Modules.Verification.Services;

public sealed class VerificationService(
    AppDbContext dbContext,
    IEmailService emailService,
    IWebHostEnvironment webHostEnvironment,
    ILogger<VerificationService> logger) : IVerificationService
{
    private static readonly TimeSpan EmailVerificationTokenLifetime = TimeSpan.FromHours(24);

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
        var token = RefreshTokenGenerator.GenerateToken();
        var expiresAt = now.Add(EmailVerificationTokenLifetime);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);

        try
        {
            await dbContext.VerificationTokens
                .Where(verificationToken =>
                    verificationToken.UserId == userId &&
                    verificationToken.ConsumedAt == null &&
                    verificationToken.ExpiresAt > now)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(
                        verificationToken => verificationToken.ConsumedAt,
                        now),
                    cancellationToken);

            dbContext.VerificationTokens.Add(new VerificationToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = RefreshTokenHasher.HashToken(token),
                ExpiresAt = expiresAt,
                CreatedAt = now
            });

            await dbContext.SaveChangesAsync(cancellationToken);
            await emailService.SendEmailVerificationAsync(
                user.Email,
                user.DisplayName,
                token,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
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

        return ApplicationResult<SendEmailVerificationResponse>.Success(new SendEmailVerificationResponse
        {
            Token = webHostEnvironment.IsDevelopment() ? token : null,
            ExpiresAt = expiresAt
        });
    }

    public async Task<ApplicationResult<VerificationSummaryResponse>> ConfirmEmailVerificationAsync(
        Guid userId,
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

        var tokenHash = RefreshTokenHasher.HashToken(token);
        var now = DateTime.UtcNow;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);

        var verificationToken = await dbContext.VerificationTokens
            .FirstOrDefaultAsync(
                currentToken =>
                    currentToken.UserId == userId &&
                    currentToken.TokenHash == tokenHash,
                cancellationToken);

        if (verificationToken is null ||
            verificationToken.ExpiresAt <= now ||
            verificationToken.ConsumedAt is not null)
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
        verification.EmailVerifiedAt = now;
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
        EmailVerifiedAt = verification.EmailVerifiedAt,
        PhoneVerifiedAt = verification.PhoneVerifiedAt,
        IdentityVerifiedAt = verification.IdentityVerifiedAt,
        TrustScore = verification.TrustScore
    };
}

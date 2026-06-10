using InteresMe.API.Data;
using InteresMe.API.Modules.Verification.Models;
using InteresMe.API.Security;
using Microsoft.EntityFrameworkCore;

namespace InteresMe.API.Modules.Verification.Services;

public sealed class VerificationTokenService(AppDbContext dbContext) : IVerificationTokenService
{
    public async Task<CreatedVerificationToken> CreateAsync(
        Guid userId,
        VerificationType type,
        string? target,
        TimeSpan lifetime,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        var token = RefreshTokenGenerator.GenerateToken();
        var expiresAt = now.Add(lifetime);

        await dbContext.VerificationTokens
            .Where(verificationToken =>
                verificationToken.UserId == userId &&
                verificationToken.Type == type &&
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
            Type = type,
            Target = target,
            TokenHash = HashToken(token),
            ExpiresAt = expiresAt,
            CreatedAt = now
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return new CreatedVerificationToken(token, expiresAt);
    }

    public string HashToken(string token) => RefreshTokenHasher.HashToken(token);

    public async Task<VerificationToken?> FindValidTokenAsync(
        string token,
        VerificationType type,
        Guid? userId,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(token);

        var query = dbContext.VerificationTokens
            .Where(currentToken =>
                currentToken.TokenHash == tokenHash &&
                currentToken.Type == type);

        if (userId is not null)
        {
            query = query.Where(currentToken => currentToken.UserId == userId.Value);
        }

        var verificationToken = await query.FirstOrDefaultAsync(cancellationToken);

        if (verificationToken is null ||
            verificationToken.ExpiresAt <= now ||
            verificationToken.ConsumedAt is not null)
        {
            return null;
        }

        return verificationToken;
    }
}

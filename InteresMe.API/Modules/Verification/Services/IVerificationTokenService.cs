using InteresMe.API.Modules.Verification.Models;

namespace InteresMe.API.Modules.Verification.Services;

public interface IVerificationTokenService
{
    Task<CreatedVerificationToken> CreateAsync(
        Guid userId,
        VerificationType type,
        string? target,
        TimeSpan lifetime,
        DateTime now,
        CancellationToken cancellationToken = default);

    string HashToken(string token);

    Task<VerificationToken?> FindValidTokenAsync(
        string token,
        VerificationType type,
        Guid? userId,
        DateTime now,
        CancellationToken cancellationToken = default);
}

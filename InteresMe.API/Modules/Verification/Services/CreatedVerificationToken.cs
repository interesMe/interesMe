namespace InteresMe.API.Modules.Verification.Services;

public sealed record CreatedVerificationToken(
    string Token,
    DateTime ExpiresAt);

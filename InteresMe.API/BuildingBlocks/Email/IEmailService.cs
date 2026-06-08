namespace InteresMe.API.BuildingBlocks.Email;

public interface IEmailService
{
    Task SendWelcomeEmailAsync(
        string email,
        string displayName,
        CancellationToken cancellationToken = default);

    Task SendEmailVerificationAsync(
        string email,
        string displayName,
        string verificationToken,
        CancellationToken cancellationToken = default);
}

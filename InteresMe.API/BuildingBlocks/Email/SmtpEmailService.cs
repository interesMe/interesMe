using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace InteresMe.API.BuildingBlocks.Email;

public sealed class SmtpEmailService(
    IOptions<EmailOptions> emailOptions,
    EmailTemplateLoader templateLoader,
    EmailTemplateRenderer templateRenderer) : IEmailService
{
    private const string WelcomeEmailHtmlTemplate = "WelcomeEmail.html";
    private const string WelcomeEmailTextTemplate = "WelcomeEmail.txt";
    private const string VerifyEmailHtmlTemplate = "VerifyEmail.html";
    private const string VerifyEmailTextTemplate = "VerifyEmail.txt";

    public async Task SendWelcomeEmailAsync(
        string email,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        await SendTemplatedEmailAsync(
            email,
            displayName,
            "Welcome to InteresMe",
            WelcomeEmailHtmlTemplate,
            WelcomeEmailTextTemplate,
            new Dictionary<string, string>
            {
                ["DisplayName"] = GetDisplayName(displayName)
            },
            cancellationToken);
    }

    public async Task SendEmailVerificationAsync(
        string email,
        string displayName,
        string verificationToken,
        CancellationToken cancellationToken = default)
    {
        await SendTemplatedEmailAsync(
            email,
            displayName,
            "Verify your email",
            VerifyEmailHtmlTemplate,
            VerifyEmailTextTemplate,
            new Dictionary<string, string>
            {
                ["DisplayName"] = GetDisplayName(displayName),
                ["VerificationToken"] = verificationToken
            },
            cancellationToken);
    }

    private async Task SendTemplatedEmailAsync(
        string email,
        string displayName,
        string subject,
        string htmlTemplate,
        string textTemplate,
        IReadOnlyDictionary<string, string> values,
        CancellationToken cancellationToken)
    {
        var options = emailOptions.Value;

        if (!IsConfigured(options))
        {
            throw new InvalidOperationException(
                "SMTP email settings are not configured.");
        }

        var htmlBody = templateRenderer.Render(
            await templateLoader.LoadAsync(htmlTemplate, cancellationToken),
            values);
        var textBody = templateRenderer.Render(
            await templateLoader.LoadAsync(textTemplate, cancellationToken),
            values);

        using var message = new MailMessage
        {
            From = new MailAddress(options.FromEmail, options.FromName),
            Subject = subject,
            Body = textBody,
            IsBodyHtml = false
        };

        message.To.Add(new MailAddress(email, displayName));
        message.AlternateViews.Add(
            AlternateView.CreateAlternateViewFromString(
                textBody,
                null,
                "text/plain"));
        message.AlternateViews.Add(
            AlternateView.CreateAlternateViewFromString(
                htmlBody,
                null,
                "text/html"));

        using var client = new SmtpClient(options.Host, options.Port)
        {
            EnableSsl = options.EnableSsl,
            Credentials = new NetworkCredential(
                options.Username,
                options.Password)
        };

        await client.SendMailAsync(message, cancellationToken);
    }

    private static bool IsConfigured(EmailOptions options) =>
        !string.IsNullOrWhiteSpace(options.Host) &&
        !string.IsNullOrWhiteSpace(options.FromEmail);

    private static string GetDisplayName(string displayName) =>
        string.IsNullOrWhiteSpace(displayName)
            ? "there"
            : displayName.Trim();
}

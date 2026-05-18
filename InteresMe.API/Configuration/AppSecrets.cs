namespace InteresMe.API.Configuration;

public static class AppSecrets
{
    public static string AuthTokenSecret { get; private set; } = string.Empty;

    public static string JwtIssuer { get; private set; } = "InteresMe";

    public static string JwtAudience { get; private set; } = "InteresMe.Client";

    public static void Load()
    {
        AuthTokenSecret = Environment.GetEnvironmentVariable("AUTH_TOKEN_SECRET") ?? string.Empty;
        JwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? JwtIssuer;
        JwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? JwtAudience;

        if (string.IsNullOrWhiteSpace(AuthTokenSecret))
        {
            throw new InvalidOperationException(
                "AUTH_TOKEN_SECRET is not set. Add it to your .env file (see .env.example).");
        }

        if (AuthTokenSecret.Length < 32)
        {
            throw new InvalidOperationException(
                "AUTH_TOKEN_SECRET must be at least 32 characters. Generate one with: openssl rand -base64 48");
        }

        var postgresPassword = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? string.Empty;
        if (postgresPassword.Length < 16)
        {
            throw new InvalidOperationException(
                "POSTGRES_PASSWORD must be at least 16 characters. Generate one with: openssl rand -base64 32");
        }

        var weakPasswords = new HashSet<string>(StringComparer.Ordinal)
        {
            "interesme",
            "change-me",
            "password",
            "postgres",
            "123456"
        };

        if (weakPasswords.Contains(postgresPassword))
        {
            throw new InvalidOperationException(
                "POSTGRES_PASSWORD is too weak. Use a unique random password in .env");
        }
    }
}

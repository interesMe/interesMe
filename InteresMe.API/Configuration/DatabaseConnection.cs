namespace InteresMe.API.Configuration;

public static class DatabaseConnection
{
    public static string Build()
    {
        var host = Required("POSTGRES_HOST");
        var port = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432";
        var database = Required("POSTGRES_DB");
        var user = Required("POSTGRES_USER");
        var password = Required("POSTGRES_PASSWORD");

        return $"Host={host};Port={port};Database={database};Username={user};Password={password}";
    }

    private static string Required(string name) =>
        Environment.GetEnvironmentVariable(name)
        ?? throw new InvalidOperationException(
            $"Environment variable '{name}' is not set. Copy .env.example to .env and configure PostgreSQL.");
}

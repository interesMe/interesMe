using InteresMe.API.Configuration;
using InteresMe.API.Data;
using InteresMe.API.Modules.Interests.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace InteresMe.API.Tests.Infrastructure;

/// <summary>
/// Creates an isolated PostgreSQL database per test class and applies the real EF Core migrations.
/// Connection is taken from INTERESME_TEST_CONNECTION, otherwise from POSTGRES_* variables (CI),
/// otherwise from a local disposable container on port 55432.
/// </summary>
public sealed class PostgresDatabase : IAsyncLifetime
{
    private readonly string databaseName = $"interesme_test_{Guid.NewGuid():N}";

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        EnsureSecretsLoaded();

        var builder = new NpgsqlConnectionStringBuilder(ResolveBaseConnectionString())
        {
            Database = databaseName
        };
        ConnectionString = builder.ConnectionString;

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
    }

    public AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .Options);

    public async Task<List<Interest>> SeedInterestsAsync(params string[] names)
    {
        await using var context = CreateContext();
        var category = new InterestCategory
        {
            Id = Guid.NewGuid(),
            Name = "Build",
            Slug = $"build-{Guid.NewGuid():N}",
            Description = "Test category",
            Icon = "BD",
            Color = "#14b8a6",
            SortOrder = 1
        };

        var interests = names
            .Select((name, index) => new Interest
            {
                Id = Guid.NewGuid(),
                CategoryId = category.Id,
                Name = name,
                Slug = $"{name.ToLowerInvariant().Replace(' ', '-')}-{Guid.NewGuid():N}",
                Description = name,
                Icon = name[..2].ToUpperInvariant(),
                Color = "#0ea5e9",
                SortOrder = index,
                CreatedAt = DateTime.UtcNow
            })
            .ToList();

        context.InterestCategories.Add(category);
        context.Interests.AddRange(interests);
        await context.SaveChangesAsync();

        return interests;
    }

    private static string ResolveBaseConnectionString()
    {
        var explicitConnection = Environment.GetEnvironmentVariable("INTERESME_TEST_CONNECTION");

        if (!string.IsNullOrWhiteSpace(explicitConnection))
        {
            return explicitConnection;
        }

        var host = Environment.GetEnvironmentVariable("POSTGRES_HOST");

        if (!string.IsNullOrWhiteSpace(host))
        {
            return new NpgsqlConnectionStringBuilder
            {
                Host = host,
                Port = int.TryParse(Environment.GetEnvironmentVariable("POSTGRES_PORT"), out var port) ? port : 5432,
                Username = Environment.GetEnvironmentVariable("POSTGRES_USER"),
                Password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD"),
                Database = "postgres"
            }.ConnectionString;
        }

        return "Host=localhost;Port=55432;Username=postgres;Password=test_only_pg_password_9f3k;Database=postgres";
    }

    private static void EnsureSecretsLoaded()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AUTH_TOKEN_SECRET")))
        {
            Environment.SetEnvironmentVariable(
                "AUTH_TOKEN_SECRET",
                "test-only-auth-token-secret-0123456789abcdef0123456789");
        }

        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("POSTGRES_PASSWORD")))
        {
            Environment.SetEnvironmentVariable("POSTGRES_PASSWORD", "test_only_pg_password_9f3k");
        }

        AppSecrets.Load();
    }
}

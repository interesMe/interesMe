using Microsoft.EntityFrameworkCore;

namespace InteresMe.API.Data;

public static class DatabaseExtensions
{
    public static async Task ApplyMigrationsAsync(this WebApplication app)
{
    using var scope = app.Services.CreateScope();

    var dbContext = scope.ServiceProvider
        .GetRequiredService<AppDbContext>();

    var logger = scope.ServiceProvider
        .GetRequiredService<ILogger<AppDbContext>>();

    try
    {
        await dbContext.Database.MigrateAsync();
        logger.LogInformation("Database migrated successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database migration failed");
        throw;
    }
}
}

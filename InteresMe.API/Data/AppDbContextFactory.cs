using InteresMe.API.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace InteresMe.API.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        EnvLoader.LoadDotEnv();
        AppSecrets.Load();

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(DatabaseConnection.Build());

        return new AppDbContext(optionsBuilder.Options);
        
    }
}

using InteresMe.API.Modules.Auth.Models;
using Microsoft.EntityFrameworkCore;

namespace InteresMe.API.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users", "auth");

            entity.HasKey(user => user.Id);

            entity.Property(user => user.Email)
                .HasMaxLength(256)
                .IsRequired();

            entity.HasIndex(user => user.Email)
                .IsUnique();

            entity.Property(user => user.DisplayName)
                .HasMaxLength(128)
                .IsRequired();

            entity.Property(user => user.PasswordHash)
                .HasMaxLength(72)
                .IsRequired();

            entity.Property(user => user.CreatedAt)
                .IsRequired();
        });
    }
}

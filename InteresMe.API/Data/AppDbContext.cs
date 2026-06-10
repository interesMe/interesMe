using InteresMe.API.Modules.Auth.Models;
using InteresMe.API.Modules.Discovery.Models;
using InteresMe.API.Modules.Interests.Models;
using InteresMe.API.Modules.Profile.Models;
using InteresMe.API.Modules.Verification.Models;
using Microsoft.EntityFrameworkCore;

namespace InteresMe.API.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    public DbSet<Interest> Interests => Set<Interest>();

    public DbSet<UserInterest> UserInterests => Set<UserInterest>();

    public DbSet<UserDiscoveryPreference> UserDiscoveryPreferences => Set<UserDiscoveryPreference>();

    public DbSet<UserVerification> UserVerifications => Set<UserVerification>();

    public DbSet<VerificationToken> VerificationTokens => Set<VerificationToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users", "auth");

            entity.HasKey(user => user.Id);

            entity.Property(user => user.Email)
                .HasMaxLength(256)
                .IsRequired();

            entity.Property(user => user.GithubId)
                .HasMaxLength(64);

            entity.HasIndex(user => user.Email)
                .IsUnique();

            entity.HasIndex(user => user.GoogleId)
                .IsUnique();

            entity.HasIndex(user => user.GithubId)
                .IsUnique();

            entity.Property(user => user.DisplayName)
                .HasMaxLength(128)
                .IsRequired();

            entity.Property(user => user.PasswordHash)
                .HasMaxLength(72)
                .IsRequired();

            entity.Property(user => user.CreatedAt)
                .IsRequired();

            entity.HasOne(user => user.Profile)
                .WithOne(profile => profile.User)
                .HasForeignKey<UserProfile>(profile => profile.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.ToTable("user_profiles", "profile");

            entity.HasKey(profile => profile.Id);

            entity.Property(profile => profile.DisplayName)
                .HasMaxLength(80)
                .IsRequired();

            entity.Property(profile => profile.Headline)
                .HasMaxLength(120);

            entity.Property(profile => profile.Bio)
                .HasMaxLength(500);

            entity.Property(profile => profile.City)
                .HasMaxLength(120);

            entity.Property(profile => profile.AvatarUrl)
                .HasMaxLength(2048);

            entity.Property(profile => profile.BirthDate);

            entity.Property(profile => profile.CreatedAt)
                .IsRequired();

            entity.Property(profile => profile.UpdatedAt)
                .IsRequired();

            entity.HasIndex(profile => profile.UserId)
                .IsUnique();
        });

        modelBuilder.Entity<Interest>(entity =>
        {
            entity.ToTable("interests", "interests");

            entity.HasKey(interest => interest.Id);

            entity.Property(interest => interest.Name)
                .HasMaxLength(80)
                .IsRequired();

            entity.Property(interest => interest.Slug)
                .HasMaxLength(100)
                .IsRequired();

            entity.HasIndex(interest => interest.Slug)
                .IsUnique();

            entity.Property(interest => interest.CreatedAt)
                .IsRequired();
        });

        modelBuilder.Entity<UserInterest>(entity =>
        {
            entity.ToTable("user_interests", "interests");

            entity.HasKey(userInterest => new
            {
                userInterest.UserId,
                userInterest.InterestId
            });

            entity.Property(userInterest => userInterest.CreatedAt)
                .IsRequired();

            entity.HasOne(userInterest => userInterest.User)
                .WithMany(user => user.UserInterests)
                .HasForeignKey(userInterest => userInterest.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(userInterest => userInterest.Interest)
                .WithMany(interest => interest.UserInterests)
                .HasForeignKey(userInterest => userInterest.InterestId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserDiscoveryPreference>(entity =>
        {
            entity.ToTable("user_discovery_preferences", "discovery");

            entity.HasKey(preference => preference.UserId);

            entity.Property(preference => preference.Goal)
                .IsRequired();

            entity.Property(preference => preference.CreatedAt)
                .IsRequired();

            entity.Property(preference => preference.UpdatedAt)
                .IsRequired();

            entity.HasOne(preference => preference.User)
                .WithOne()
                .HasForeignKey<UserDiscoveryPreference>(preference => preference.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });


        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens", "auth");

            entity.HasKey(token => token.Id);

            entity.Property(token => token.TokenHash)
                .HasMaxLength(512)
                .IsRequired();

            entity.Property(token => token.TokenHash)
                .IsRequired();

            entity.Property(token => token.CreatedAt)
                .IsRequired();

            entity.Property(token => token.ExpiresAt)
                .IsRequired();

            entity.HasOne(token => token.user)
                .WithMany(user => user.RefreshTokens)
                .HasForeignKey(token => token.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserVerification>(entity =>
        {
            entity.ToTable("user_verifications", "verification");

            entity.HasKey(verification => verification.UserId);

            entity.Property(verification => verification.TrustScore)
                .IsRequired();

            entity.Property(verification => verification.PhoneNumber)
                .HasMaxLength(32);

            entity.Property(verification => verification.CreatedAt)
                .IsRequired();

            entity.Property(verification => verification.UpdatedAt)
                .IsRequired();

            entity.HasOne(verification => verification.User)
                .WithOne()
                .HasForeignKey<UserVerification>(verification => verification.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<VerificationToken>(entity =>
        {
            entity.ToTable("verification_tokens", "verification");

            entity.HasKey(token => token.Id);

            entity.Property(token => token.Type)
                .IsRequired();

            entity.Property(token => token.Target)
                .HasMaxLength(256);

            entity.Property(token => token.TokenHash)
                .HasMaxLength(512)
                .IsRequired();

            entity.Property(token => token.ExpiresAt)
                .IsRequired();

            entity.Property(token => token.CreatedAt)
                .IsRequired();

            entity.HasIndex(token => token.TokenHash);

            entity.HasIndex(token => token.UserId);

            entity.HasOne(token => token.User)
                .WithMany()
                .HasForeignKey(token => token.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

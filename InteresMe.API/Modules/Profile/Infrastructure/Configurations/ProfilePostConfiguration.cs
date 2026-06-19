using InteresMe.API.Modules.Profile.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InteresMe.API.Modules.Profile.Infrastructure.Configurations;

public sealed class ProfilePostConfiguration : IEntityTypeConfiguration<ProfilePost>
{
    public void Configure(EntityTypeBuilder<ProfilePost> builder)
    {
        builder.ToTable("profile_posts", "profile");

        builder.HasKey(post => post.Id);

        builder.Property(post => post.Title)
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(post => post.Body)
            .HasMaxLength(1200)
            .IsRequired();

        builder.Property(post => post.Type)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(post => post.InterestPath)
            .HasMaxLength(240);

        builder.Property(post => post.MediaUrlsJson)
            .HasColumnType("jsonb");

        builder.Property(post => post.CreatedAt)
            .IsRequired();

        builder.HasIndex(post => new { post.UserId, post.CreatedAt })
            .IsDescending(false, true);

        builder.HasOne(post => post.User)
            .WithMany()
            .HasForeignKey(post => post.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

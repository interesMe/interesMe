using InteresMe.API.Modules.Interests.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InteresMe.API.Modules.Interests.Infrastructure.Configurations;

public sealed class InterestConfiguration : IEntityTypeConfiguration<Interest>
{
    public void Configure(EntityTypeBuilder<Interest> builder)
    {
        builder.ToTable("interests", "interests");

        builder.HasKey(interest => interest.Id);

        builder.Property(interest => interest.Name)
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(interest => interest.Slug)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(interest => interest.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(interest => interest.Icon)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(interest => interest.Color)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(interest => interest.SortOrder)
            .IsRequired();

        builder.Property(interest => interest.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(interest => interest.CreatedAt)
            .IsRequired();

        builder.HasIndex(interest => interest.Slug)
            .IsUnique();

        builder.HasIndex(interest => interest.CategoryId);

        builder.HasMany(interest => interest.Subinterests)
            .WithOne(subinterest => subinterest.Interest)
            .HasForeignKey(subinterest => subinterest.InterestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

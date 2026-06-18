using InteresMe.API.Modules.Interests.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InteresMe.API.Modules.Interests.Infrastructure.Configurations;

public sealed class InterestCategoryConfiguration : IEntityTypeConfiguration<InterestCategory>
{
    public void Configure(EntityTypeBuilder<InterestCategory> builder)
    {
        builder.ToTable("interest_categories", "interests");

        builder.HasKey(category => category.Id);

        builder.Property(category => category.Name)
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(category => category.Slug)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(category => category.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(category => category.Icon)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(category => category.Color)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(category => category.SortOrder)
            .IsRequired();

        builder.Property(category => category.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.HasIndex(category => category.Slug)
            .IsUnique();

        builder.HasMany(category => category.Interests)
            .WithOne(interest => interest.Category)
            .HasForeignKey(interest => interest.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

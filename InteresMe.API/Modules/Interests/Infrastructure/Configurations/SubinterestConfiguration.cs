using InteresMe.API.Modules.Interests.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InteresMe.API.Modules.Interests.Infrastructure.Configurations;

public sealed class SubinterestConfiguration : IEntityTypeConfiguration<Subinterest>
{
    public void Configure(EntityTypeBuilder<Subinterest> builder)
    {
        builder.ToTable("subinterests", "interests");

        builder.HasKey(subinterest => subinterest.Id);

        builder.Property(subinterest => subinterest.Name)
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(subinterest => subinterest.Slug)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(subinterest => subinterest.Description)
            .HasMaxLength(500);

        builder.Property(subinterest => subinterest.SortOrder)
            .IsRequired();

        builder.Property(subinterest => subinterest.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.HasIndex(subinterest => new
            {
                subinterest.InterestId,
                subinterest.Slug
            })
            .IsUnique();
    }
}

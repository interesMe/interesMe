using InteresMe.API.Modules.Interests.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InteresMe.API.Modules.Interests.Infrastructure.Configurations;

public sealed class UserSubinterestConfiguration : IEntityTypeConfiguration<UserSubinterest>
{
    public void Configure(EntityTypeBuilder<UserSubinterest> builder)
    {
        builder.ToTable("user_subinterests", "interests");

        builder.HasKey(selection => new { selection.UserId, selection.SubinterestId });

        builder.Property(selection => selection.CreatedAt)
            .IsRequired();

        builder.HasIndex(selection => selection.SubinterestId);

        builder.HasOne(selection => selection.User)
            .WithMany()
            .HasForeignKey(selection => selection.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(selection => selection.Subinterest)
            .WithMany()
            .HasForeignKey(selection => selection.SubinterestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

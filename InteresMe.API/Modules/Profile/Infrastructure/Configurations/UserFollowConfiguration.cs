using InteresMe.API.Modules.Profile.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InteresMe.API.Modules.Profile.Infrastructure.Configurations;

public sealed class UserFollowConfiguration : IEntityTypeConfiguration<UserFollow>
{
    public void Configure(EntityTypeBuilder<UserFollow> builder)
    {
        builder.ToTable("user_follows", "profile");

        builder.HasKey(follow => new { follow.FollowerId, follow.FollowedId });

        builder.Property(follow => follow.CreatedAt)
            .IsRequired();

        builder.HasIndex(follow => follow.FollowedId);

        builder.HasOne(follow => follow.Follower)
            .WithMany()
            .HasForeignKey(follow => follow.FollowerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(follow => follow.Followed)
            .WithMany()
            .HasForeignKey(follow => follow.FollowedId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

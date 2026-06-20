using InteresMe.API.Modules.Posts.Likes.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InteresMe.API.Modules.Posts.Likes.Infrastructure;

public sealed class PostLikeConfiguration : IEntityTypeConfiguration<PostLike>
{
    public void Configure(EntityTypeBuilder<PostLike> builder)
    {
        builder.ToTable("likes", "posts");

        builder.HasKey(like => new { like.PostId, like.UserId });

        builder.Property(like => like.CreatedAt)
            .IsRequired();

        builder.HasIndex(like => like.UserId);

        builder.HasOne(like => like.Post)
            .WithMany()
            .HasForeignKey(like => like.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(like => like.User)
            .WithMany()
            .HasForeignKey(like => like.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

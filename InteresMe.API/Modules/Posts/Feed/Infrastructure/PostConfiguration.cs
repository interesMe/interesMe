using InteresMe.API.Modules.Posts.Feed.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InteresMe.API.Modules.Posts.Feed.Infrastructure;

public sealed class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.ToTable("posts", "posts");

        builder.HasKey(post => post.Id);

        builder.Property(post => post.Body)
            .HasMaxLength(PostFeedRules.MaxBodyLength)
            .IsRequired();

        builder.Property(post => post.CreatedAt)
            .IsRequired();

        builder.HasIndex(post => new { post.AuthorId, post.CreatedAt, post.Id })
            .IsDescending(false, true, true);

        builder.HasOne(post => post.Author)
            .WithMany()
            .HasForeignKey(post => post.AuthorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(post => post.Initiative)
            .WithMany()
            .HasForeignKey(post => post.InitiativeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

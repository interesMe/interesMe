using InteresMe.API.Modules.Posts.Comments.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InteresMe.API.Modules.Posts.Comments.Infrastructure;

public sealed class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.ToTable("comments", "posts");

        builder.HasKey(comment => comment.Id);

        builder.Property(comment => comment.Body)
            .HasMaxLength(CommentRules.MaxBodyLength)
            .IsRequired();

        builder.Property(comment => comment.CreatedAt)
            .IsRequired();

        builder.HasIndex(comment => new { comment.PostId, comment.CreatedAt, comment.Id });

        builder.HasIndex(comment => new { comment.ParentCommentId, comment.CreatedAt, comment.Id });

        builder.HasIndex(comment => comment.AuthorId);

        builder.HasOne(comment => comment.Post)
            .WithMany()
            .HasForeignKey(comment => comment.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(comment => comment.Author)
            .WithMany()
            .HasForeignKey(comment => comment.AuthorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(comment => comment.ParentComment)
            .WithMany(comment => comment.Replies)
            .HasForeignKey(comment => comment.ParentCommentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

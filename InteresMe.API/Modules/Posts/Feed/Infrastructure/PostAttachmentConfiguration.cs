using InteresMe.API.Modules.Posts.Feed.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InteresMe.API.Modules.Posts.Feed.Infrastructure;

public sealed class PostAttachmentConfiguration : IEntityTypeConfiguration<PostAttachment>
{
    public void Configure(EntityTypeBuilder<PostAttachment> builder)
    {
        builder.ToTable("post_attachments", "posts", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "CK_post_attachments_SizeBytes_positive",
                "\"SizeBytes\" > 0");
            tableBuilder.HasCheckConstraint(
                "CK_post_attachments_SortOrder_nonnegative",
                "\"SortOrder\" >= 0");
        });

        builder.HasKey(attachment => attachment.Id);

        builder.Property(attachment => attachment.Kind)
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(attachment => attachment.StoragePath)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(attachment => attachment.ContentType)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(attachment => attachment.SizeBytes)
            .IsRequired();

        builder.Property(attachment => attachment.SortOrder)
            .IsRequired();

        builder.Property(attachment => attachment.CreatedAt)
            .IsRequired();

        builder.HasIndex(attachment => new { attachment.PostId, attachment.SortOrder })
            .IsUnique();

        builder.HasOne(attachment => attachment.Post)
            .WithMany(post => post.Attachments)
            .HasForeignKey(attachment => attachment.PostId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

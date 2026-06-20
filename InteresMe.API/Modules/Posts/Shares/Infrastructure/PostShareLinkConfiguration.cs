using InteresMe.API.Modules.Posts.Shares.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InteresMe.API.Modules.Posts.Shares.Infrastructure;

public sealed class PostShareLinkConfiguration : IEntityTypeConfiguration<PostShareLink>
{
    public void Configure(EntityTypeBuilder<PostShareLink> builder)
    {
        builder.ToTable("share_links", "posts");

        builder.HasKey(link => link.PostId);

        builder.Property(link => link.Token)
            .HasMaxLength(PostShareRules.MaxTokenLength)
            .IsRequired();

        builder.Property(link => link.CreatedAt)
            .IsRequired();

        builder.HasIndex(link => link.Token)
            .IsUnique();

        builder.HasOne(link => link.Post)
            .WithOne()
            .HasForeignKey<PostShareLink>(link => link.PostId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

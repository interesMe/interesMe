using InteresMe.API.Modules.History.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InteresMe.API.Modules.History.Infrastructure.Configurations;

public sealed class UserHistoryEventConfiguration : IEntityTypeConfiguration<UserHistoryEvent>
{
    public void Configure(EntityTypeBuilder<UserHistoryEvent> builder)
    {
        builder.ToTable("user_history_events", "history");

        builder.HasKey(historyEvent => historyEvent.Id);

        builder.Property(historyEvent => historyEvent.Type)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(historyEvent => historyEvent.Title)
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(historyEvent => historyEvent.Description)
            .HasMaxLength(600);

        builder.Property(historyEvent => historyEvent.TargetType)
            .HasMaxLength(64);

        builder.Property(historyEvent => historyEvent.TargetName)
            .HasMaxLength(160);

        builder.Property(historyEvent => historyEvent.MetadataJson)
            .HasColumnType("jsonb");

        builder.Property(historyEvent => historyEvent.OccurredAt)
            .IsRequired();

        builder.Property(historyEvent => historyEvent.CreatedAt)
            .IsRequired();

        builder.HasIndex(historyEvent => new
            {
                historyEvent.UserId,
                historyEvent.OccurredAt
            })
            .IsDescending(false, true);

        builder.HasOne(historyEvent => historyEvent.User)
            .WithMany()
            .HasForeignKey(historyEvent => historyEvent.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

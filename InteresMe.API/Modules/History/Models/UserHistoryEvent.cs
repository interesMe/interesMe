using InteresMe.API.Modules.Auth.Models;

namespace InteresMe.API.Modules.History.Models;

public sealed class UserHistoryEvent
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? TargetType { get; set; }

    public Guid? TargetId { get; set; }

    public string? TargetName { get; set; }

    public string? MetadataJson { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public User User { get; set; } = null!;
}

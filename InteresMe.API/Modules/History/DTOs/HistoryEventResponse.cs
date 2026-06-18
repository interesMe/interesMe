namespace InteresMe.API.Modules.History.DTOs;

public sealed class HistoryEventResponse
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Date { get; set; } = string.Empty;

    public string Icon { get; set; } = string.Empty;

    public string Accent { get; set; } = string.Empty;

    public string? TargetType { get; set; }

    public Guid? TargetId { get; set; }

    public string? TargetName { get; set; }

    public string? MetadataJson { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

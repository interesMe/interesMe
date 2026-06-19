namespace InteresMe.API.Modules.Profile.DTOs.ProfileView;

public sealed class ProfileHistoryPreviewResponse
{
    public Guid Id { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? TargetType { get; set; }

    public Guid? TargetId { get; set; }

    public string? TargetName { get; set; }

    public string? MetadataJson { get; set; }

    public DateTimeOffset OccurredAt { get; set; }
}

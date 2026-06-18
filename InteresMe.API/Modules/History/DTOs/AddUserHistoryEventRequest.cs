namespace InteresMe.API.Modules.History.DTOs;

public sealed record AddUserHistoryEventRequest(
    Guid UserId,
    string Type,
    string Title,
    string? Description = null,
    string? TargetType = null,
    Guid? TargetId = null,
    string? TargetName = null,
    string? MetadataJson = null,
    DateTimeOffset? OccurredAt = null);

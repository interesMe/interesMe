using InteresMe.API.Data;
using InteresMe.API.Modules.History.DTOs;
using InteresMe.API.Modules.History.Models;
using Microsoft.EntityFrameworkCore;

namespace InteresMe.API.Modules.History.Services;

public sealed class UserHistoryService(AppDbContext dbContext) : IUserHistoryService
{
    private const int MaxPageSize = 100;

    public async Task AddEventAsync(
        AddUserHistoryEventRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!HistoryEventTypes.IsSupported(request.Type))
        {
            throw new ArgumentException($"Unsupported history event type '{request.Type}'.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ArgumentException("History event title is required.", nameof(request));
        }

        var now = DateTimeOffset.UtcNow;

        dbContext.UserHistoryEvents.Add(new UserHistoryEvent
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Type = request.Type,
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description)
                ? null
                : request.Description.Trim(),
            TargetType = string.IsNullOrWhiteSpace(request.TargetType)
                ? null
                : request.TargetType.Trim(),
            TargetId = request.TargetId,
            TargetName = string.IsNullOrWhiteSpace(request.TargetName)
                ? null
                : request.TargetName.Trim(),
            MetadataJson = string.IsNullOrWhiteSpace(request.MetadataJson)
                ? null
                : request.MetadataJson,
            OccurredAt = request.OccurredAt ?? now,
            CreatedAt = now
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<HistoryResponse> GetHistoryAsync(
        Guid userId,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var historyEvents = await dbContext.UserHistoryEvents
            .AsNoTracking()
            .Where(historyEvent => historyEvent.UserId == userId)
            .OrderByDescending(historyEvent => historyEvent.OccurredAt)
            .ThenByDescending(historyEvent => historyEvent.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new HistoryResponse
        {
            Events = historyEvents.Select(ToResponse).ToList()
        };
    }

    private static HistoryEventResponse ToResponse(UserHistoryEvent historyEvent) => new()
    {
        Id = historyEvent.Id,
        UserId = historyEvent.UserId,
        Type = historyEvent.Type,
        Title = historyEvent.Title,
        Description = historyEvent.Description,
        Date = historyEvent.OccurredAt.ToString("O"),
        Icon = GetIcon(historyEvent.Type),
        Accent = GetAccent(historyEvent.Type),
        TargetType = historyEvent.TargetType,
        TargetId = historyEvent.TargetId,
        TargetName = historyEvent.TargetName,
        MetadataJson = historyEvent.MetadataJson,
        OccurredAt = historyEvent.OccurredAt,
        CreatedAt = historyEvent.CreatedAt
    };

    private static string GetIcon(string type) =>
        type switch
        {
            HistoryEventTypes.CreatedInitiative => "rocket",
            HistoryEventTypes.JoinedInitiative => "users",
            HistoryEventTypes.CompletedInitiative => "check-circle",
            HistoryEventTypes.AddedInterest => "sparkles",
            HistoryEventTypes.JoinedCommunity => "network",
            HistoryEventTypes.OrganizedEvent => "calendar",
            HistoryEventTypes.CreatedPost => "file-text",
            HistoryEventTypes.AchievementUnlocked => "trophy",
            HistoryEventTypes.ReceivedInvitation => "mail",
            _ => "clock"
        };

    private static string GetAccent(string type) =>
        type switch
        {
            HistoryEventTypes.CreatedInitiative => "#14b8a6",
            HistoryEventTypes.JoinedInitiative => "#38bdf8",
            HistoryEventTypes.CompletedInitiative => "#22c55e",
            HistoryEventTypes.AddedInterest => "#2dd4bf",
            HistoryEventTypes.JoinedCommunity => "#818cf8",
            HistoryEventTypes.OrganizedEvent => "#f59e0b",
            HistoryEventTypes.CreatedPost => "#fb7185",
            HistoryEventTypes.AchievementUnlocked => "#facc15",
            HistoryEventTypes.ReceivedInvitation => "#a78bfa",
            _ => "#64748b"
        };
}

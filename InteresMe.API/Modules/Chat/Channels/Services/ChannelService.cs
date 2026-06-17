using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.Data;
using InteresMe.API.Modules.Chat.Channels.DTOs;
using InteresMe.API.Modules.Chat.Channels.Models;
using InteresMe.API.Modules.Chat.Shared;
using InteresMe.API.Modules.Chat.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace InteresMe.API.Modules.Chat.Channels.Services;

public sealed class ChannelService(AppDbContext dbContext) : IChannelService
{
    private const int ChannelTitleMaxLength = 160;

    public async Task<List<ChannelDto>> GetChannelsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var channels = await BaseChannelQuery()
            .Where(channel => channel.Members.Any(member => member.UserId == userId))
            .OrderByDescending(channel => channel.UpdatedAt)
            .ToListAsync(cancellationToken);

        return channels.Select(ToDto).ToList();
    }

    public async Task<ApplicationResult<ChannelDto>> CreateChannelAsync(
        Guid userId,
        CreateChannelRequest? request,
        CancellationToken cancellationToken = default)
    {
        var title = request?.Title?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(title))
        {
            return ApplicationResult<ChannelDto>.Failure(
                ApplicationErrorKind.Validation,
                ChatErrorCodes.ChannelTitleRequired,
                "Channel title is required.");
        }

        if (title.Length > ChannelTitleMaxLength)
        {
            return ApplicationResult<ChannelDto>.Failure(
                ApplicationErrorKind.Validation,
                ChatErrorCodes.ChannelTitleTooLong,
                $"Channel title must be at most {ChannelTitleMaxLength} characters.");
        }

        if (request?.InitiativeId is not null)
        {
            var initiativeExists = await dbContext.Initiatives
                .AsNoTracking()
                .AnyAsync(
                    initiative => initiative.Id == request.InitiativeId.Value,
                    cancellationToken);

            if (!initiativeExists)
            {
                return ApplicationResult<ChannelDto>.Failure(
                    ApplicationErrorKind.NotFound,
                    ChatErrorCodes.ChannelInitiativeNotFound,
                    "Initiative was not found.");
            }
        }

        var now = DateTime.UtcNow;
        var channel = new Channel
        {
            Id = Guid.NewGuid(),
            InitiativeId = request?.InitiativeId,
            Title = title,
            CreatedAt = now,
            UpdatedAt = now,
            Members =
            [
                new ChannelMember
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    JoinedAt = now,
                    Role = ChatParticipantRole.Owner
                }
            ]
        };

        dbContext.Channels.Add(channel);
        await dbContext.SaveChangesAsync(cancellationToken);

        var created = await BaseChannelQuery()
            .FirstAsync(currentChannel => currentChannel.Id == channel.Id, cancellationToken);

        return ApplicationResult<ChannelDto>.Success(ToDto(created));
    }

    public async Task<ApplicationResult<ChannelDto>> GetChannelAsync(
        Guid userId,
        Guid channelId,
        CancellationToken cancellationToken = default)
    {
        var channel = await BaseChannelQuery()
            .FirstOrDefaultAsync(
                currentChannel => currentChannel.Id == channelId,
                cancellationToken);

        if (channel is null)
        {
            return ApplicationResult<ChannelDto>.Failure(
                ApplicationErrorKind.NotFound,
                ChatErrorCodes.ChannelNotFound,
                "Channel was not found.");
        }

        if (!IsMember(channel, userId))
        {
            return ApplicationResult<ChannelDto>.Failure(
                ApplicationErrorKind.Forbidden,
                ChatErrorCodes.ChannelAccessDenied,
                "You do not have access to this channel.");
        }

        return ApplicationResult<ChannelDto>.Success(ToDto(channel));
    }

    public async Task<ApplicationResult<ChannelDto>> AddMemberAsync(
        Guid userId,
        Guid channelId,
        Guid memberUserId,
        CancellationToken cancellationToken = default)
    {
        var channel = await dbContext.Channels
            .Include(currentChannel => currentChannel.Members)
                .ThenInclude(member => member.User)
            .FirstOrDefaultAsync(
                currentChannel => currentChannel.Id == channelId,
                cancellationToken);

        if (channel is null)
        {
            return ApplicationResult<ChannelDto>.Failure(
                ApplicationErrorKind.NotFound,
                ChatErrorCodes.ChannelNotFound,
                "Channel was not found.");
        }

        if (!IsMember(channel, userId))
        {
            return ApplicationResult<ChannelDto>.Failure(
                ApplicationErrorKind.Forbidden,
                ChatErrorCodes.ChannelAccessDenied,
                "You do not have access to this channel.");
        }

        var userExists = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(user => user.Id == memberUserId, cancellationToken);

        if (!userExists)
        {
            return ApplicationResult<ChannelDto>.Failure(
                ApplicationErrorKind.NotFound,
                ChatErrorCodes.ChannelMemberNotFound,
                "User was not found.");
        }

        if (!IsMember(channel, memberUserId))
        {
            channel.Members.Add(new ChannelMember
            {
                Id = Guid.NewGuid(),
                ChannelId = channelId,
                UserId = memberUserId,
                JoinedAt = DateTime.UtcNow,
                Role = ChatParticipantRole.Member
            });

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return ApplicationResult<ChannelDto>.Success(ToDto(channel));
    }

    private IQueryable<Channel> BaseChannelQuery() =>
        dbContext.Channels
            .AsNoTracking()
            .AsSplitQuery()
            .Include(channel => channel.Members)
                .ThenInclude(member => member.User);

    private static ChannelDto ToDto(Channel channel) => new()
    {
        Id = channel.Id,
        InitiativeId = channel.InitiativeId,
        Title = channel.Title,
        UpdatedAt = channel.UpdatedAt,
        MembersCount = channel.Members.Count,
        Members = channel.Members
            .OrderBy(member => member.JoinedAt)
            .Select(member => new ChannelMemberDto
            {
                UserId = member.UserId,
                DisplayName = member.User?.DisplayName ?? string.Empty,
                Role = member.Role,
                JoinedAt = member.JoinedAt
            })
            .ToList()
    };

    private static bool IsMember(Channel channel, Guid userId) =>
        channel.Members.Any(member => member.UserId == userId);
}

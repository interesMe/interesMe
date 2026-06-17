using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.Data;
using InteresMe.API.Modules.Chat.Groups.DTOs;
using InteresMe.API.Modules.Chat.Groups.Models;
using InteresMe.API.Modules.Chat.Shared;
using InteresMe.API.Modules.Chat.Shared.DTOs;
using InteresMe.API.Modules.Chat.Shared.Enums;
using InteresMe.API.Modules.Chat.Shared.Models;
using InteresMe.API.Modules.Chat.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace InteresMe.API.Modules.Chat.Groups.Services;

public sealed class GroupChatService(AppDbContext dbContext) : IGroupChatService
{
    public async Task<List<GroupChatDto>> GetGroupChatsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var groupChats = await BaseGroupChatQuery()
            .Where(groupChat => groupChat.Participants.Any(participant => participant.UserId == userId))
            .OrderByDescending(groupChat => groupChat.UpdatedAt)
            .ToListAsync(cancellationToken);

        return groupChats.Select(ToDto).ToList();
    }

    public async Task<ApplicationResult<GroupChatDto>> GetOrCreateInitiativeGroupChatAsync(
        Guid userId,
        Guid initiativeId,
        CancellationToken cancellationToken = default)
    {
        var initiative = await dbContext.Initiatives
            .AsNoTracking()
            .FirstOrDefaultAsync(
                currentInitiative => currentInitiative.Id == initiativeId,
                cancellationToken);

        if (initiative is null)
        {
            return ApplicationResult<GroupChatDto>.Failure(
                ApplicationErrorKind.NotFound,
                ChatErrorCodes.GroupNotFound,
                "Initiative was not found.");
        }

        var existingGroupChat = await dbContext.GroupChats
            .Include(groupChat => groupChat.Participants)
                .ThenInclude(participant => participant.User)
            .Include(groupChat => groupChat.Messages.OrderByDescending(message => message.CreatedAt).Take(1))
            .FirstOrDefaultAsync(
                groupChat => groupChat.InitiativeId == initiativeId,
                cancellationToken);

        if (existingGroupChat is not null)
        {
            if (!IsParticipant(existingGroupChat, userId))
            {
                existingGroupChat.Participants.Add(new GroupChatParticipant
                {
                    Id = Guid.NewGuid(),
                    GroupChatId = existingGroupChat.Id,
                    UserId = userId,
                    JoinedAt = DateTime.UtcNow,
                    Role = initiative.OwnerUserId == userId
                        ? ChatParticipantRole.Owner
                        : ChatParticipantRole.Member
                });

                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return ApplicationResult<GroupChatDto>.Success(ToDto(existingGroupChat));
        }

        var now = DateTime.UtcNow;
        var groupChat = new GroupChat
        {
            Id = Guid.NewGuid(),
            InitiativeId = initiativeId,
            Title = string.IsNullOrWhiteSpace(initiative.Title)
                ? "Initiative group chat"
                : initiative.Title,
            CreatedAt = now,
            UpdatedAt = now,
            Participants =
            [
                new GroupChatParticipant
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    JoinedAt = now,
                    Role = initiative.OwnerUserId == userId
                        ? ChatParticipantRole.Owner
                        : ChatParticipantRole.Member
                }
            ]
        };

        dbContext.GroupChats.Add(groupChat);
        await dbContext.SaveChangesAsync(cancellationToken);

        var created = await BaseGroupChatQuery()
            .FirstAsync(currentGroupChat => currentGroupChat.Id == groupChat.Id, cancellationToken);

        return ApplicationResult<GroupChatDto>.Success(ToDto(created));
    }

    public async Task<ApplicationResult<GroupChatDto>> GetGroupChatAsync(
        Guid userId,
        Guid groupChatId,
        CancellationToken cancellationToken = default)
    {
        var groupChat = await BaseGroupChatQuery()
            .FirstOrDefaultAsync(
                currentGroupChat => currentGroupChat.Id == groupChatId,
                cancellationToken);

        if (groupChat is null)
        {
            return ApplicationResult<GroupChatDto>.Failure(
                ApplicationErrorKind.NotFound,
                ChatErrorCodes.GroupNotFound,
                "Group chat was not found.");
        }

        if (!IsParticipant(groupChat, userId))
        {
            return ApplicationResult<GroupChatDto>.Failure(
                ApplicationErrorKind.Forbidden,
                ChatErrorCodes.GroupAccessDenied,
                "You do not have access to this group chat.");
        }

        return ApplicationResult<GroupChatDto>.Success(ToDto(groupChat));
    }

    public async Task<ApplicationResult<List<ChatMessageDto>>> GetMessagesAsync(
        Guid userId,
        Guid groupChatId,
        int take,
        CancellationToken cancellationToken = default)
    {
        var accessError = await VerifyParticipantAccessAsync(userId, groupChatId, cancellationToken);

        if (accessError is not null)
        {
            return ApplicationResult<List<ChatMessageDto>>.Failure(
                accessError.Value.Kind,
                accessError.Value.Code,
                accessError.Value.Message);
        }

        var messages = await dbContext.ChatMessages
            .AsNoTracking()
            .Include(message => message.SenderUser)
            .Where(message => message.GroupChatId == groupChatId)
            .OrderByDescending(message => message.CreatedAt)
            .Take(ChatMessageRules.NormalizeTake(take))
            .OrderBy(message => message.CreatedAt)
            .ToListAsync(cancellationToken);

        return ApplicationResult<List<ChatMessageDto>>.Success(
            messages.Select(ChatMessageMapper.ToDto).ToList());
    }

    public async Task<ApplicationResult<ChatMessageDto>> SendMessageAsync(
        Guid userId,
        Guid groupChatId,
        SendMessageRequest? request,
        CancellationToken cancellationToken = default)
    {
        var text = ChatMessageRules.NormalizeMessageText(request?.Text);

        if (string.IsNullOrWhiteSpace(text))
        {
            return ApplicationResult<ChatMessageDto>.Failure(
                ApplicationErrorKind.Validation,
                ChatErrorCodes.MessageEmpty,
                "Message text is required.");
        }

        if (text.Length > ChatMessageRules.MessageMaxLength)
        {
            return ApplicationResult<ChatMessageDto>.Failure(
                ApplicationErrorKind.Validation,
                ChatErrorCodes.MessageTooLong,
                $"Message text must be at most {ChatMessageRules.MessageMaxLength} characters.");
        }

        var groupChat = await dbContext.GroupChats
            .Include(currentGroupChat => currentGroupChat.Participants)
            .FirstOrDefaultAsync(
                currentGroupChat => currentGroupChat.Id == groupChatId,
                cancellationToken);

        if (groupChat is null)
        {
            return ApplicationResult<ChatMessageDto>.Failure(
                ApplicationErrorKind.NotFound,
                ChatErrorCodes.GroupNotFound,
                "Group chat was not found.");
        }

        if (!IsParticipant(groupChat, userId))
        {
            return ApplicationResult<ChatMessageDto>.Failure(
                ApplicationErrorKind.Forbidden,
                ChatErrorCodes.GroupAccessDenied,
                "You do not have access to this group chat.");
        }

        var now = DateTime.UtcNow;
        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ConversationType = ChatConversationType.Group,
            GroupChatId = groupChatId,
            SenderUserId = userId,
            Text = text,
            CreatedAt = now,
            IsDeleted = false
        };

        groupChat.UpdatedAt = now;
        dbContext.ChatMessages.Add(message);
        await dbContext.SaveChangesAsync(cancellationToken);

        await dbContext.Entry(message)
            .Reference(currentMessage => currentMessage.SenderUser)
            .LoadAsync(cancellationToken);

        return ApplicationResult<ChatMessageDto>.Success(ChatMessageMapper.ToDto(message));
    }

    public async Task<ApplicationResult<GroupChatDto>> AddParticipantAsync(
        Guid userId,
        Guid groupChatId,
        Guid participantUserId,
        CancellationToken cancellationToken = default)
    {
        var groupChat = await dbContext.GroupChats
            .Include(currentGroupChat => currentGroupChat.Participants)
                .ThenInclude(participant => participant.User)
            .Include(currentGroupChat => currentGroupChat.Messages.OrderByDescending(message => message.CreatedAt).Take(1))
            .FirstOrDefaultAsync(
                currentGroupChat => currentGroupChat.Id == groupChatId,
                cancellationToken);

        if (groupChat is null)
        {
            return ApplicationResult<GroupChatDto>.Failure(
                ApplicationErrorKind.NotFound,
                ChatErrorCodes.GroupNotFound,
                "Group chat was not found.");
        }

        if (!IsParticipant(groupChat, userId))
        {
            return ApplicationResult<GroupChatDto>.Failure(
                ApplicationErrorKind.Forbidden,
                ChatErrorCodes.GroupAccessDenied,
                "You do not have access to this group chat.");
        }

        var userExists = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(user => user.Id == participantUserId, cancellationToken);

        if (!userExists)
        {
            return ApplicationResult<GroupChatDto>.Failure(
                ApplicationErrorKind.NotFound,
                ChatErrorCodes.GroupParticipantNotFound,
                "User was not found.");
        }

        if (!IsParticipant(groupChat, participantUserId))
        {
            groupChat.Participants.Add(new GroupChatParticipant
            {
                Id = Guid.NewGuid(),
                GroupChatId = groupChatId,
                UserId = participantUserId,
                JoinedAt = DateTime.UtcNow,
                Role = ChatParticipantRole.Member
            });

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return ApplicationResult<GroupChatDto>.Success(ToDto(groupChat));
    }

    private IQueryable<GroupChat> BaseGroupChatQuery() =>
        dbContext.GroupChats
            .AsNoTracking()
            .AsSplitQuery()
            .Include(groupChat => groupChat.Participants)
                .ThenInclude(participant => participant.User)
            .Include(groupChat => groupChat.Messages.OrderByDescending(message => message.CreatedAt).Take(1));

    private async Task<AccessError?> VerifyParticipantAccessAsync(
        Guid userId,
        Guid groupChatId,
        CancellationToken cancellationToken)
    {
        var groupChatExists = await dbContext.GroupChats
            .AsNoTracking()
            .AnyAsync(groupChat => groupChat.Id == groupChatId, cancellationToken);

        if (!groupChatExists)
        {
            return new AccessError(
                ApplicationErrorKind.NotFound,
                ChatErrorCodes.GroupNotFound,
                "Group chat was not found.");
        }

        var isParticipant = await dbContext.GroupChatParticipants
            .AsNoTracking()
            .AnyAsync(
                participant =>
                    participant.GroupChatId == groupChatId &&
                    participant.UserId == userId,
                cancellationToken);

        return isParticipant
            ? null
            : new AccessError(
                ApplicationErrorKind.Forbidden,
                ChatErrorCodes.GroupAccessDenied,
                "You do not have access to this group chat.");
    }

    private static GroupChatDto ToDto(GroupChat groupChat)
    {
        var lastMessage = groupChat.Messages
            .OrderByDescending(message => message.CreatedAt)
            .FirstOrDefault();

        return new GroupChatDto
        {
            Id = groupChat.Id,
            InitiativeId = groupChat.InitiativeId,
            Title = groupChat.Title,
            UpdatedAt = groupChat.UpdatedAt,
            ParticipantsCount = groupChat.Participants.Count,
            LastMessagePreview = lastMessage is null
                ? null
                : ChatMessageRules.ToPreview(lastMessage.Text),
            Participants = groupChat.Participants
                .OrderBy(participant => participant.JoinedAt)
                .Select(participant => new GroupChatParticipantDto
                {
                    UserId = participant.UserId,
                    DisplayName = participant.User?.DisplayName ?? string.Empty,
                    Role = participant.Role,
                    JoinedAt = participant.JoinedAt
                })
                .ToList()
        };
    }

    private static bool IsParticipant(GroupChat groupChat, Guid userId) =>
        groupChat.Participants.Any(participant => participant.UserId == userId);

    private readonly record struct AccessError(
        ApplicationErrorKind Kind,
        string Code,
        string Message);
}

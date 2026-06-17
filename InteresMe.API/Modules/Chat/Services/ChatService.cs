using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.Data;
using InteresMe.API.Modules.Chat.DTOs;
using InteresMe.API.Modules.Chat.Models;
using Microsoft.EntityFrameworkCore;

namespace InteresMe.API.Modules.Chat.Services;

public sealed class ChatService(AppDbContext dbContext) : IChatService
{
    private const int MessageMaxLength = 2000;
    private const int MessagesDefaultTake = 50;
    private const int MessagesMaxTake = 100;
    private const int LastMessagePreviewMaxLength = 140;

    public async Task<List<ChatDto>> GetChatsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var chats = await BaseChatQuery()
            .Where(chat => chat.Participants.Any(participant => participant.UserId == userId))
            .OrderByDescending(chat => chat.UpdatedAt)
            .ToListAsync(cancellationToken);

        return chats
            .Select(ToChatDto)
            .ToList();
    }

    public async Task<ApplicationResult<ChatDto>> GetChatAsync(
        Guid userId,
        Guid chatId,
        CancellationToken cancellationToken = default)
    {
        var chat = await BaseChatQuery()
            .FirstOrDefaultAsync(
                currentChat => currentChat.Id == chatId,
                cancellationToken);

        if (chat is null)
        {
            return ApplicationResult<ChatDto>.Failure(
                ApplicationErrorKind.NotFound,
                "Chat was not found.");
        }

        if (!IsParticipant(chat, userId))
        {
            return ApplicationResult<ChatDto>.Failure(
                ApplicationErrorKind.Forbidden,
                "You do not have access to this chat.");
        }

        return ApplicationResult<ChatDto>.Success(ToChatDto(chat));
    }

    public async Task<ApplicationResult<List<ChatMessageDto>>> GetMessagesAsync(
        Guid userId,
        Guid chatId,
        int take,
        CancellationToken cancellationToken = default)
    {
        var accessError = await VerifyParticipantAccessAsync(
            userId,
            chatId,
            cancellationToken);

        if (accessError is not null)
        {
            return ApplicationResult<List<ChatMessageDto>>.Failure(
                accessError.Value.Kind,
                accessError.Value.Message);
        }

        var normalizedTake = NormalizeTake(take);

        var messages = await dbContext.ChatMessages
            .AsNoTracking()
            .Include(message => message.SenderUser)
            .Where(message => message.ChatRoomId == chatId)
            .OrderByDescending(message => message.CreatedAt)
            .Take(normalizedTake)
            .OrderBy(message => message.CreatedAt)
            .ToListAsync(cancellationToken);

        return ApplicationResult<List<ChatMessageDto>>.Success(
            messages.Select(ToMessageDto).ToList());
    }

    public async Task<ApplicationResult<ChatMessageDto>> SendMessageAsync(
        Guid userId,
        Guid chatId,
        SendMessageRequest? request,
        CancellationToken cancellationToken = default)
    {
        var normalizedText = NormalizeMessageText(request?.Text);

        if (string.IsNullOrWhiteSpace(normalizedText))
        {
            return ApplicationResult<ChatMessageDto>.Failure(
                ApplicationErrorKind.Validation,
                "Message text is required.");
        }

        if (normalizedText.Length > MessageMaxLength)
        {
            return ApplicationResult<ChatMessageDto>.Failure(
                ApplicationErrorKind.Validation,
                $"Message text must be at most {MessageMaxLength} characters.");
        }

        var chat = await dbContext.ChatRooms
            .Include(currentChat => currentChat.Participants)
            .FirstOrDefaultAsync(
                currentChat => currentChat.Id == chatId,
                cancellationToken);

        if (chat is null)
        {
            return ApplicationResult<ChatMessageDto>.Failure(
                ApplicationErrorKind.NotFound,
                "Chat was not found.");
        }

        if (!IsParticipant(chat, userId))
        {
            return ApplicationResult<ChatMessageDto>.Failure(
                ApplicationErrorKind.Forbidden,
                "You do not have access to this chat.");
        }

        var now = DateTime.UtcNow;
        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ChatRoomId = chatId,
            SenderUserId = userId,
            Text = normalizedText,
            CreatedAt = now,
            IsDeleted = false
        };

        chat.UpdatedAt = now;
        dbContext.ChatMessages.Add(message);
        await dbContext.SaveChangesAsync(cancellationToken);

        await dbContext.Entry(message)
            .Reference(currentMessage => currentMessage.SenderUser)
            .LoadAsync(cancellationToken);

        return ApplicationResult<ChatMessageDto>.Success(ToMessageDto(message));
    }

    public async Task<ApplicationResult<ChatDto>> GetOrCreateInitiativeChatAsync(
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
            return ApplicationResult<ChatDto>.Failure(
                ApplicationErrorKind.NotFound,
                "Initiative was not found.");
        }

        var existingChat = await dbContext.ChatRooms
            .Include(chat => chat.Participants)
                .ThenInclude(participant => participant.User)
            .Include(chat => chat.Messages.OrderByDescending(message => message.CreatedAt).Take(1))
            .FirstOrDefaultAsync(
                chat => chat.InitiativeId == initiativeId,
                cancellationToken);

        if (existingChat is not null)
        {
            if (!IsParticipant(existingChat, userId))
            {
                existingChat.Participants.Add(new ChatParticipant
                {
                    Id = Guid.NewGuid(),
                    ChatRoomId = existingChat.Id,
                    UserId = userId,
                    JoinedAt = DateTime.UtcNow,
                    Role = initiative.OwnerUserId == userId
                        ? ChatParticipantRole.Owner
                        : ChatParticipantRole.Member
                });

                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return ApplicationResult<ChatDto>.Success(ToChatDto(existingChat));
        }

        var now = DateTime.UtcNow;
        var chatRoom = new ChatRoom
        {
            Id = Guid.NewGuid(),
            InitiativeId = initiativeId,
            Title = string.IsNullOrWhiteSpace(initiative.Title)
                ? "Initiative chat"
                : initiative.Title,
            CreatedAt = now,
            UpdatedAt = now,
            Participants =
            [
                new ChatParticipant
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

        dbContext.ChatRooms.Add(chatRoom);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ApplicationResult<ChatDto>.Success(ToChatDto(chatRoom));
    }

    public async Task<ApplicationResult<ChatDto>> AddParticipantAsync(
        Guid userId,
        Guid chatId,
        Guid participantUserId,
        CancellationToken cancellationToken = default)
    {
        var chat = await dbContext.ChatRooms
            .Include(currentChat => currentChat.Participants)
                .ThenInclude(participant => participant.User)
            .Include(currentChat => currentChat.Messages.OrderByDescending(message => message.CreatedAt).Take(1))
            .FirstOrDefaultAsync(
                currentChat => currentChat.Id == chatId,
                cancellationToken);

        if (chat is null)
        {
            return ApplicationResult<ChatDto>.Failure(
                ApplicationErrorKind.NotFound,
                "Chat was not found.");
        }

        if (!IsParticipant(chat, userId))
        {
            return ApplicationResult<ChatDto>.Failure(
                ApplicationErrorKind.Forbidden,
                "You do not have access to this chat.");
        }

        var userExists = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(user => user.Id == participantUserId, cancellationToken);

        if (!userExists)
        {
            return ApplicationResult<ChatDto>.Failure(
                ApplicationErrorKind.NotFound,
                "User was not found.");
        }

        if (!IsParticipant(chat, participantUserId))
        {
            chat.Participants.Add(new ChatParticipant
            {
                Id = Guid.NewGuid(),
                ChatRoomId = chatId,
                UserId = participantUserId,
                JoinedAt = DateTime.UtcNow,
                Role = ChatParticipantRole.Member
            });

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return ApplicationResult<ChatDto>.Success(ToChatDto(chat));
    }

    private IQueryable<ChatRoom> BaseChatQuery() =>
        dbContext.ChatRooms
            .AsNoTracking()
            .AsSplitQuery()
            .Include(chat => chat.Participants)
                .ThenInclude(participant => participant.User)
            .Include(chat => chat.Messages.OrderByDescending(message => message.CreatedAt).Take(1));

    private async Task<AccessError?> VerifyParticipantAccessAsync(
        Guid userId,
        Guid chatId,
        CancellationToken cancellationToken)
    {
        var chatExists = await dbContext.ChatRooms
            .AsNoTracking()
            .AnyAsync(chat => chat.Id == chatId, cancellationToken);

        if (!chatExists)
        {
            return new AccessError(
                ApplicationErrorKind.NotFound,
                "Chat was not found.");
        }

        var isParticipant = await dbContext.ChatParticipants
            .AsNoTracking()
            .AnyAsync(
                participant =>
                    participant.ChatRoomId == chatId &&
                    participant.UserId == userId,
                cancellationToken);

        return isParticipant
            ? null
            : new AccessError(
                ApplicationErrorKind.Forbidden,
                "You do not have access to this chat.");
    }

    private static bool IsParticipant(ChatRoom chat, Guid userId) =>
        chat.Participants.Any(participant => participant.UserId == userId);

    private static int NormalizeTake(int take) =>
        take <= 0
            ? MessagesDefaultTake
            : Math.Min(take, MessagesMaxTake);

    private static string NormalizeMessageText(string? text) =>
        text?.Trim() ?? string.Empty;

    private static ChatDto ToChatDto(ChatRoom chat)
    {
        var lastMessage = chat.Messages
            .OrderByDescending(message => message.CreatedAt)
            .FirstOrDefault();

        return new ChatDto
        {
            Id = chat.Id,
            InitiativeId = chat.InitiativeId,
            Title = chat.Title,
            UpdatedAt = chat.UpdatedAt,
            ParticipantsCount = chat.Participants.Count,
            LastMessagePreview = lastMessage is null
                ? null
                : ToPreview(lastMessage.Text),
            Participants = chat.Participants
                .OrderBy(participant => participant.JoinedAt)
                .Select(participant => new ChatParticipantDto
                {
                    UserId = participant.UserId,
                    DisplayName = participant.User?.DisplayName ?? string.Empty,
                    Role = participant.Role,
                    JoinedAt = participant.JoinedAt
                })
                .ToList()
        };
    }

    private static ChatMessageDto ToMessageDto(ChatMessage message) => new()
    {
        Id = message.Id,
        ChatRoomId = message.ChatRoomId,
        SenderUserId = message.SenderUserId,
        SenderDisplayName = message.SenderUser?.DisplayName,
        Text = message.Text,
        CreatedAt = message.CreatedAt,
        EditedAt = message.EditedAt,
        IsDeleted = message.IsDeleted
    };

    private static string ToPreview(string text) =>
        text.Length <= LastMessagePreviewMaxLength
            ? text
            : string.Concat(text.AsSpan(0, LastMessagePreviewMaxLength), "...");

    private readonly record struct AccessError(
        ApplicationErrorKind Kind,
        string Message);
}

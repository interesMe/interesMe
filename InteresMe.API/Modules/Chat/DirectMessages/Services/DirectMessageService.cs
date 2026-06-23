using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.Data;
using InteresMe.API.Modules.Chat.DirectMessages.DTOs;
using InteresMe.API.Modules.Chat.DirectMessages.Models;
using InteresMe.API.Modules.Chat.Shared;
using InteresMe.API.Modules.Chat.Shared.DTOs;
using InteresMe.API.Modules.Chat.Shared.Enums;
using InteresMe.API.Modules.Chat.Shared.Models;
using InteresMe.API.Modules.Chat.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace InteresMe.API.Modules.Chat.DirectMessages.Services;

public sealed class DirectMessageService(
    AppDbContext dbContext,
    IChatMessageAttachmentValidator attachmentValidator,
    IChatMessageAttachmentStorage attachmentStorage) : IDirectMessageService
{
    public async Task<List<DirectConversationDto>> GetConversationsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var conversations = await BaseConversationQuery()
            .Where(conversation => conversation.Participants.Any(participant => participant.UserId == userId))
            .OrderByDescending(conversation => conversation.UpdatedAt)
            .ToListAsync(cancellationToken);

        return conversations
            .Select(ToDto)
            .ToList();
    }

    public async Task<ApplicationResult<DirectConversationDto>> GetOrCreateConversationAsync(
        Guid userId,
        Guid otherUserId,
        CancellationToken cancellationToken = default)
    {
        if (userId == otherUserId)
        {
            return ApplicationResult<DirectConversationDto>.Failure(
                ApplicationErrorKind.Validation,
                ChatErrorCodes.DirectSelfNotAllowed,
                "Direct conversation with yourself is not allowed.");
        }

        var otherUserExists = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(user => user.Id == otherUserId, cancellationToken);

        if (!otherUserExists)
        {
            return ApplicationResult<DirectConversationDto>.Failure(
                ApplicationErrorKind.NotFound,
                ChatErrorCodes.DirectUserNotFound,
                "User was not found.");
        }

        var (userOneId, userTwoId) = NormalizePair(userId, otherUserId);

        var existingConversation = await BaseConversationQuery()
            .FirstOrDefaultAsync(
                conversation =>
                    conversation.UserOneId == userOneId &&
                    conversation.UserTwoId == userTwoId,
                cancellationToken);

        if (existingConversation is not null)
        {
            return ApplicationResult<DirectConversationDto>.Success(
                ToDto(existingConversation));
        }

        var now = DateTime.UtcNow;
        var conversation = new DirectConversation
        {
            Id = Guid.NewGuid(),
            UserOneId = userOneId,
            UserTwoId = userTwoId,
            CreatedAt = now,
            UpdatedAt = now,
            Participants =
            [
                new DirectConversationParticipant
                {
                    Id = Guid.NewGuid(),
                    UserId = userOneId,
                    JoinedAt = now
                },
                new DirectConversationParticipant
                {
                    Id = Guid.NewGuid(),
                    UserId = userTwoId,
                    JoinedAt = now
                }
            ]
        };

        dbContext.DirectConversations.Add(conversation);
        await dbContext.SaveChangesAsync(cancellationToken);

        var created = await BaseConversationQuery()
            .FirstAsync(
                currentConversation => currentConversation.Id == conversation.Id,
                cancellationToken);

        return ApplicationResult<DirectConversationDto>.Success(ToDto(created));
    }

    public async Task<ApplicationResult<List<ChatMessageDto>>> GetMessagesAsync(
        Guid userId,
        Guid conversationId,
        int take,
        CancellationToken cancellationToken = default)
    {
        var accessError = await VerifyParticipantAccessAsync(
            userId,
            conversationId,
            cancellationToken);

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
            .Include(message => message.Attachments)
            .Where(message => message.DirectConversationId == conversationId)
            .OrderByDescending(message => message.CreatedAt)
            .Take(ChatMessageRules.NormalizeTake(take))
            .OrderBy(message => message.CreatedAt)
            .ToListAsync(cancellationToken);

        return ApplicationResult<List<ChatMessageDto>>.Success(
            messages.Select(ChatMessageMapper.ToDto).ToList());
    }

    public async Task<ApplicationResult<ChatMessageDto>> SendMessageAsync(
        Guid userId,
        Guid conversationId,
        string? text,
        IReadOnlyList<IFormFile> attachments,
        CancellationToken cancellationToken = default)
    {
        var normalizedText = ChatMessageRules.NormalizeMessageText(text);

        if (string.IsNullOrWhiteSpace(normalizedText) && attachments.Count == 0)
        {
            return ApplicationResult<ChatMessageDto>.Failure(
                ApplicationErrorKind.Validation,
                ChatErrorCodes.MessageEmpty,
                "Message text or an image attachment is required.");
        }

        if (normalizedText.Length > ChatMessageRules.MessageMaxLength)
        {
            return ApplicationResult<ChatMessageDto>.Failure(
                ApplicationErrorKind.Validation,
                ChatErrorCodes.MessageTooLong,
                $"Message text must be at most {ChatMessageRules.MessageMaxLength} characters.");
        }

        var conversation = await dbContext.DirectConversations
            .Include(currentConversation => currentConversation.Participants)
            .FirstOrDefaultAsync(
                currentConversation => currentConversation.Id == conversationId,
                cancellationToken);

        if (conversation is null)
        {
            return ApplicationResult<ChatMessageDto>.Failure(
                ApplicationErrorKind.NotFound,
                ChatErrorCodes.DirectConversationNotFound,
                "Direct conversation was not found.");
        }

        if (!IsParticipant(conversation, userId))
        {
            return ApplicationResult<ChatMessageDto>.Failure(
                ApplicationErrorKind.Forbidden,
                ChatErrorCodes.DirectAccessDenied,
                "You do not have access to this direct conversation.");
        }

        var attachmentValidation = await attachmentValidator.ValidateAsync(attachments, cancellationToken);
        if (!attachmentValidation.IsSuccess)
        {
            return ApplicationResult<ChatMessageDto>.Failure(
                attachmentValidation.ErrorKind ?? ApplicationErrorKind.Validation,
                attachmentValidation.ErrorCode ?? ChatErrorCodes.MessageValidationFailed,
                attachmentValidation.ErrorMessage ?? "Message attachments are invalid.");
        }

        var now = DateTime.UtcNow;
        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ConversationType = ChatConversationType.Direct,
            DirectConversationId = conversationId,
            SenderUserId = userId,
            Text = string.IsNullOrWhiteSpace(normalizedText) ? null : normalizedText,
            CreatedAt = now,
            IsDeleted = false
        };

        IReadOnlyList<StagedChatMessageAttachment> stagedAttachments = [];
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            stagedAttachments = await attachmentStorage.StageAsync(
                conversationId,
                message.Id,
                attachmentValidation.Response ?? [],
                cancellationToken);

            foreach (var attachment in stagedAttachments.OrderBy(current => current.SortOrder))
            {
                message.Attachments.Add(new ChatMessageAttachment
                {
                    Id = attachment.Id,
                    ChatMessageId = message.Id,
                    Url = attachment.StoragePath,
                    FileName = attachment.FileName,
                    ContentType = attachment.ContentType,
                    SizeBytes = attachment.SizeBytes,
                    CreatedAt = now
                });
            }

            conversation.UpdatedAt = now;
            dbContext.ChatMessages.Add(message);
            await dbContext.SaveChangesAsync(cancellationToken);

            attachmentStorage.MoveToFinal(stagedAttachments);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            attachmentStorage.CleanupAll(stagedAttachments);
            throw;
        }

        await dbContext.Entry(message)
            .Reference(currentMessage => currentMessage.SenderUser)
            .LoadAsync(cancellationToken);

        return ApplicationResult<ChatMessageDto>.Success(
            ChatMessageMapper.ToDto(message));
    }

    private IQueryable<DirectConversation> BaseConversationQuery() =>
        dbContext.DirectConversations
            .AsNoTracking()
            .AsSplitQuery()
            .Include(conversation => conversation.Participants)
                .ThenInclude(participant => participant.User)
            .Include(conversation => conversation.Messages.OrderByDescending(message => message.CreatedAt).Take(1))
                .ThenInclude(message => message.Attachments);

    private async Task<AccessError?> VerifyParticipantAccessAsync(
        Guid userId,
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        var conversationExists = await dbContext.DirectConversations
            .AsNoTracking()
            .AnyAsync(conversation => conversation.Id == conversationId, cancellationToken);

        if (!conversationExists)
        {
            return new AccessError(
                ApplicationErrorKind.NotFound,
                ChatErrorCodes.DirectConversationNotFound,
                "Direct conversation was not found.");
        }

        var isParticipant = await dbContext.DirectConversationParticipants
            .AsNoTracking()
            .AnyAsync(
                participant =>
                    participant.DirectConversationId == conversationId &&
                    participant.UserId == userId,
                cancellationToken);

        return isParticipant
            ? null
            : new AccessError(
                ApplicationErrorKind.Forbidden,
                ChatErrorCodes.DirectAccessDenied,
                "You do not have access to this direct conversation.");
    }

    private static DirectConversationDto ToDto(DirectConversation conversation)
    {
        var lastMessage = conversation.Messages
            .OrderByDescending(message => message.CreatedAt)
            .FirstOrDefault();

        return new DirectConversationDto
        {
            Id = conversation.Id,
            UpdatedAt = conversation.UpdatedAt,
            ParticipantsCount = conversation.Participants.Count,
            LastMessagePreview = lastMessage is null
                ? null
                : ChatMessageRules.ToPreview(lastMessage.Text, lastMessage.Attachments.Count > 0),
            Participants = conversation.Participants
                .OrderBy(participant => participant.JoinedAt)
                .Select(participant => new DirectConversationParticipantDto
                {
                    UserId = participant.UserId,
                    DisplayName = participant.User?.DisplayName ?? string.Empty,
                    JoinedAt = participant.JoinedAt
                })
                .ToList()
        };
    }

    private static bool IsParticipant(DirectConversation conversation, Guid userId) =>
        conversation.Participants.Any(participant => participant.UserId == userId);

    private static (Guid UserOneId, Guid UserTwoId) NormalizePair(Guid firstUserId, Guid secondUserId) =>
        firstUserId.CompareTo(secondUserId) <= 0
            ? (firstUserId, secondUserId)
            : (secondUserId, firstUserId);

    private readonly record struct AccessError(
        ApplicationErrorKind Kind,
        string Code,
        string Message);
}

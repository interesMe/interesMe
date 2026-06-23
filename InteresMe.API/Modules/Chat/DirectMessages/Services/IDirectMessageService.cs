using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.Modules.Chat.DirectMessages.DTOs;
using InteresMe.API.Modules.Chat.Shared.DTOs;

namespace InteresMe.API.Modules.Chat.DirectMessages.Services;

public interface IDirectMessageService
{
    Task<List<DirectConversationDto>> GetConversationsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<ApplicationResult<DirectConversationDto>> GetOrCreateConversationAsync(
        Guid userId,
        Guid otherUserId,
        CancellationToken cancellationToken = default);

    Task<ApplicationResult<List<ChatMessageDto>>> GetMessagesAsync(
        Guid userId,
        Guid conversationId,
        int take,
        CancellationToken cancellationToken = default);

    Task<ApplicationResult<ChatMessageDto>> SendMessageAsync(
        Guid userId,
        Guid conversationId,
        string? text,
        IReadOnlyList<IFormFile> attachments,
        CancellationToken cancellationToken = default);
}

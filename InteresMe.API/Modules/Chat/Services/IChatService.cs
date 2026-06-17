using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.Modules.Chat.DTOs;

namespace InteresMe.API.Modules.Chat.Services;

public interface IChatService
{
    Task<List<ChatDto>> GetChatsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<ApplicationResult<ChatDto>> GetChatAsync(
        Guid userId,
        Guid chatId,
        CancellationToken cancellationToken = default);

    Task<ApplicationResult<List<ChatMessageDto>>> GetMessagesAsync(
        Guid userId,
        Guid chatId,
        int take,
        CancellationToken cancellationToken = default);

    Task<ApplicationResult<ChatMessageDto>> SendMessageAsync(
        Guid userId,
        Guid chatId,
        SendMessageRequest? request,
        CancellationToken cancellationToken = default);

    Task<ApplicationResult<ChatDto>> GetOrCreateInitiativeChatAsync(
        Guid userId,
        Guid initiativeId,
        CancellationToken cancellationToken = default);

    Task<ApplicationResult<ChatDto>> AddParticipantAsync(
        Guid userId,
        Guid chatId,
        Guid participantUserId,
        CancellationToken cancellationToken = default);
}

using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.Modules.Chat.Groups.DTOs;
using InteresMe.API.Modules.Chat.Shared.DTOs;

namespace InteresMe.API.Modules.Chat.Groups.Services;

public interface IGroupChatService
{
    Task<List<GroupChatDto>> GetGroupChatsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<ApplicationResult<GroupChatDto>> GetOrCreateInitiativeGroupChatAsync(
        Guid userId,
        Guid initiativeId,
        CancellationToken cancellationToken = default);

    Task<ApplicationResult<GroupChatDto>> GetGroupChatAsync(
        Guid userId,
        Guid groupChatId,
        CancellationToken cancellationToken = default);

    Task<ApplicationResult<List<ChatMessageDto>>> GetMessagesAsync(
        Guid userId,
        Guid groupChatId,
        int take,
        CancellationToken cancellationToken = default);

    Task<ApplicationResult<ChatMessageDto>> SendMessageAsync(
        Guid userId,
        Guid groupChatId,
        SendMessageRequest? request,
        CancellationToken cancellationToken = default);

    Task<ApplicationResult<GroupChatDto>> AddParticipantAsync(
        Guid userId,
        Guid groupChatId,
        Guid participantUserId,
        CancellationToken cancellationToken = default);
}

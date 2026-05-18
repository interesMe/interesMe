using InteresMe.API.Modules.Chat.DTOs;

namespace InteresMe.API.Modules.Chat.Services;

public interface IChatService
{
    Task<IReadOnlyList<MessageResponse>> GetConversationAsync(Guid userId, Guid otherUserId);

    Task<MessageResponse?> SendAsync(SendMessageRequest request);
}

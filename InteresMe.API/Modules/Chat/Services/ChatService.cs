using System.Collections.Concurrent;
using InteresMe.API.Modules.Chat.DTOs;
using InteresMe.API.Modules.Chat.Models;
using InteresMe.API.Modules.Users.Services;

namespace InteresMe.API.Modules.Chat.Services;

public class ChatService(IUserService userService) : IChatService
{
    private readonly ConcurrentDictionary<Guid, Message> _messages = new();

    public Task<IReadOnlyList<MessageResponse>> GetConversationAsync(Guid userId, Guid otherUserId)
    {
        var conversation = _messages.Values
            .Where(message =>
                (message.SenderId == userId && message.ReceiverId == otherUserId) ||
                (message.SenderId == otherUserId && message.ReceiverId == userId))
            .OrderBy(message => message.SentAt)
            .Select(ToResponse)
            .ToList();

        return Task.FromResult<IReadOnlyList<MessageResponse>>(conversation);
    }

    public async Task<MessageResponse?> SendAsync(SendMessageRequest request)
    {
        if (request.SenderId == Guid.Empty ||
            request.ReceiverId == Guid.Empty ||
            string.IsNullOrWhiteSpace(request.Content))
        {
            return null;
        }

        var sender = await userService.GetByIdAsync(request.SenderId);
        var receiver = await userService.GetByIdAsync(request.ReceiverId);

        if (sender is null || receiver is null)
        {
            return null;
        }

        var message = new Message
        {
            Id = Guid.NewGuid(),
            SenderId = request.SenderId,
            ReceiverId = request.ReceiverId,
            Content = request.Content.Trim(),
            SentAt = DateTime.UtcNow
        };

        _messages[message.Id] = message;
        return ToResponse(message);
    }

    private static MessageResponse ToResponse(Message message) => new()
    {
        Id = message.Id,
        SenderId = message.SenderId,
        ReceiverId = message.ReceiverId,
        Content = message.Content,
        SentAt = message.SentAt
    };
}

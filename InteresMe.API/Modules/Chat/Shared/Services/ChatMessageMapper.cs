using InteresMe.API.Modules.Chat.Shared.DTOs;
using InteresMe.API.Modules.Chat.Shared.Models;

namespace InteresMe.API.Modules.Chat.Shared.Services;

public static class ChatMessageMapper
{
    public static ChatMessageDto ToDto(ChatMessage message) => new()
    {
        Id = message.Id,
        ConversationType = message.ConversationType,
        DirectConversationId = message.DirectConversationId,
        GroupChatId = message.GroupChatId,
        ChannelId = message.ChannelId,
        SenderUserId = message.SenderUserId,
        SenderDisplayName = message.SenderUser?.DisplayName,
        Text = message.Text,
        CreatedAt = message.CreatedAt,
        EditedAt = message.EditedAt,
        IsDeleted = message.IsDeleted
    };
}

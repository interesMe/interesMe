using InteresMe.API.Modules.Auth.Models;
using InteresMe.API.Modules.Chat.Channels.Models;
using InteresMe.API.Modules.Chat.DirectMessages.Models;
using InteresMe.API.Modules.Chat.Groups.Models;
using InteresMe.API.Modules.Chat.Shared.Enums;

namespace InteresMe.API.Modules.Chat.Shared.Models;

public sealed class ChatMessage
{
    public Guid Id { get; set; }

    public ChatConversationType ConversationType { get; set; }

    public Guid? DirectConversationId { get; set; }

    public Guid? GroupChatId { get; set; }

    public Guid? ChannelId { get; set; }

    public Guid SenderUserId { get; set; }

    public string? Text { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? EditedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DirectConversation? DirectConversation { get; set; }

    public GroupChat? GroupChat { get; set; }

    public Channel? Channel { get; set; }

    public User SenderUser { get; set; } = null!;

    public ICollection<ChatMessageAttachment> Attachments { get; set; } = [];
}

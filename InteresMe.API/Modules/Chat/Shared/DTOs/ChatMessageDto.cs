using InteresMe.API.Modules.Chat.Shared.Enums;

namespace InteresMe.API.Modules.Chat.Shared.DTOs;

public sealed class ChatMessageDto
{
    public Guid Id { get; set; }

    public ChatConversationType ConversationType { get; set; }

    public Guid? DirectConversationId { get; set; }

    public Guid? GroupChatId { get; set; }

    public Guid? ChannelId { get; set; }

    public Guid SenderUserId { get; set; }

    public string? SenderDisplayName { get; set; }

    public string? Text { get; set; }

    public IReadOnlyList<ChatMessageAttachmentDto> Attachments { get; set; } = [];

    public DateTime CreatedAt { get; set; }

    public DateTime? EditedAt { get; set; }

    public bool IsDeleted { get; set; }
}

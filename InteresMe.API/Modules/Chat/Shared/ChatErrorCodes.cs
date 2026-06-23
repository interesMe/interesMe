namespace InteresMe.API.Modules.Chat.Shared;

public static class ChatErrorCodes
{
    public const string DirectSelfNotAllowed = "chat.direct.self_not_allowed";
    public const string DirectUserNotFound = "chat.direct.user_not_found";
    public const string DirectConversationNotFound = "chat.direct.conversation_not_found";
    public const string DirectAccessDenied = "chat.direct.access_denied";

    public const string GroupNotFound = "chat.group.not_found";
    public const string GroupAccessDenied = "chat.group.access_denied";
    public const string GroupParticipantNotFound = "chat.group.participant_not_found";

    public const string ChannelTitleRequired = "chat.channel.title_required";
    public const string ChannelTitleTooLong = "chat.channel.title_too_long";
    public const string ChannelInitiativeNotFound = "chat.channel.initiative_not_found";
    public const string ChannelNotFound = "chat.channel.not_found";
    public const string ChannelAccessDenied = "chat.channel.access_denied";
    public const string ChannelMemberNotFound = "chat.channel.member_not_found";

    public const string MessageEmpty = "chat.message.empty";
    public const string MessageTooLong = "chat.message.too_long";
    public const string MessageValidationFailed = "chat.message.validation_failed";
    public const string AttachmentInvalidType = "chat.message.attachment.invalid_type";
    public const string AttachmentTooLarge = "chat.message.attachment.too_large";
    public const string AttachmentTooMany = "chat.message.attachment.too_many";
    public const string AttachmentTotalTooLarge = "chat.message.attachment.total_too_large";
    public const string AttachmentEmpty = "chat.message.attachment.empty";
}

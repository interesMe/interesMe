namespace InteresMe.API.Modules.Chat.Shared.Services;

public interface IChatMessageAttachmentStorage
{
    Task<IReadOnlyList<StagedChatMessageAttachment>> StageAsync(
        Guid conversationId,
        Guid messageId,
        IReadOnlyList<ValidatedChatMessageAttachment> attachments,
        CancellationToken cancellationToken = default);

    void MoveToFinal(IReadOnlyList<StagedChatMessageAttachment> attachments);

    void CleanupAll(IReadOnlyList<StagedChatMessageAttachment> attachments);
}

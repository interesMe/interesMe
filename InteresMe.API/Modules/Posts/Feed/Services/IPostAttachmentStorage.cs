namespace InteresMe.API.Modules.Posts.Feed.Services;

public interface IPostAttachmentStorage
{
    Task<IReadOnlyList<StagedPostAttachment>> StageAsync(
        Guid authorId,
        Guid postId,
        IReadOnlyList<ValidatedPostAttachment> attachments,
        CancellationToken cancellationToken = default);

    void MoveToFinal(IReadOnlyList<StagedPostAttachment> attachments);

    void CleanupAll(IReadOnlyList<StagedPostAttachment> attachments);
}

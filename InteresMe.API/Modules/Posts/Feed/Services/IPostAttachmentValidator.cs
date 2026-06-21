using InteresMe.API.BuildingBlocks.Results;

namespace InteresMe.API.Modules.Posts.Feed.Services;

public interface IPostAttachmentValidator
{
    Task<ApplicationResult<IReadOnlyList<ValidatedPostAttachment>>> ValidateAsync(
        IReadOnlyList<IFormFile> files,
        CancellationToken cancellationToken = default);
}

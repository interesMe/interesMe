using InteresMe.API.BuildingBlocks.Results;

namespace InteresMe.API.Modules.Chat.Shared.Services;

public interface IChatMessageAttachmentValidator
{
    Task<ApplicationResult<IReadOnlyList<ValidatedChatMessageAttachment>>> ValidateAsync(
        IReadOnlyList<IFormFile> files,
        CancellationToken cancellationToken = default);
}

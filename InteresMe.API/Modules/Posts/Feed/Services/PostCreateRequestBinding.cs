using InteresMe.API.Modules.Posts.Feed.Contracts.Requests;

namespace InteresMe.API.Modules.Posts.Feed.Services;

public sealed record PostCreateRequestBinding(
    CreatePostRequest? Request,
    IReadOnlyList<IFormFile> Attachments,
    string? ErrorCode = null,
    string? ErrorMessage = null);

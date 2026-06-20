using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.Modules.Posts.Feed.Contracts.Requests;
using InteresMe.API.Modules.Posts.Feed.Contracts.Responses;

namespace InteresMe.API.Modules.Posts.Feed.Services;

public interface IPostFeedService
{
    Task<ApplicationResult<PostResponse>> CreateAsync(
        Guid authorId,
        CreatePostRequest? request,
        CancellationToken cancellationToken = default);

    Task<ApplicationResult<PostResponse>> GetByIdAsync(
        Guid viewerUserId,
        Guid postId,
        CancellationToken cancellationToken = default);

    Task<ApplicationResult<PostPageResponse>> GetUserPostsAsync(
        Guid viewerUserId,
        Guid userId,
        string? cursor,
        int pageSize = PostFeedRules.DefaultPageSize,
        CancellationToken cancellationToken = default);
}

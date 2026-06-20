using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.Modules.Posts.Likes.Contracts.Responses;

namespace InteresMe.API.Modules.Posts.Likes.Services;

public interface IPostLikeService
{
    Task<ApplicationResult<LikeResponse>> ToggleAsync(
        Guid userId,
        Guid postId,
        CancellationToken cancellationToken = default);

    Task<ApplicationResult<LikeResponse>> UnlikeAsync(
        Guid userId,
        Guid postId,
        CancellationToken cancellationToken = default);
}

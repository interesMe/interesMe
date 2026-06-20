using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.Modules.Posts.Comments.Contracts.Requests;
using InteresMe.API.Modules.Posts.Comments.Contracts.Responses;

namespace InteresMe.API.Modules.Posts.Comments.Services;

public interface ICommentService
{
    Task<ApplicationResult<CommentResponse>> CreateAsync(
        Guid authorId,
        Guid postId,
        CreateCommentRequest? request,
        CancellationToken cancellationToken = default);

    Task<ApplicationResult<CommentPageResponse>> GetForPostAsync(
        Guid postId,
        string? cursor,
        int pageSize = CommentRules.DefaultPageSize,
        CancellationToken cancellationToken = default);

    Task<ApplicationResult<bool>> DeleteAsync(
        Guid authorId,
        Guid postId,
        Guid commentId,
        CancellationToken cancellationToken = default);
}

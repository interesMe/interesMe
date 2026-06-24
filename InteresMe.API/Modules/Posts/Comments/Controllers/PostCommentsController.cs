using InteresMe.API.BuildingBlocks.Security;
using InteresMe.API.Modules.Posts.Comments.Contracts.Requests;
using InteresMe.API.Modules.Posts.Comments.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static InteresMe.API.BuildingBlocks.Results.ApplicationResultMapper;

namespace InteresMe.API.Modules.Posts.Comments.Controllers;

[ApiController]
[Authorize]
[Route("api/posts/{postId}/comments")]
public sealed class PostCommentsController(
    ICommentService commentService,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        Guid postId,
        [FromBody] CreateCommentRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await commentService.CreateAsync(
            currentUser.UserId,
            postId,
            request,
            cancellationToken);

        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, result.Response)
            : ToActionResult(result);
    }

    [HttpPost("{commentId}/replies")]
    public async Task<IActionResult> CreateReply(
        Guid postId,
        Guid commentId,
        [FromBody] CreateCommentRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await commentService.CreateReplyAsync(
            currentUser.UserId,
            postId,
            commentId,
            request,
            cancellationToken);

        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, result.Response)
            : ToActionResult(result);
    }

    [HttpGet]
    public Task<IActionResult> GetForPost(
        Guid postId,
        [FromQuery] string? cursor = null,
        [FromQuery] int pageSize = CommentRules.DefaultPageSize,
        CancellationToken cancellationToken = default) =>
        ToActionResult(commentService.GetForPostAsync(
            postId,
            cursor,
            pageSize,
            cancellationToken));

    [HttpDelete("{commentId}")]
    public async Task<IActionResult> Delete(
        Guid postId,
        Guid commentId,
        CancellationToken cancellationToken)
    {
        var result = await commentService.DeleteAsync(
            currentUser.UserId,
            postId,
            commentId,
            cancellationToken);

        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }
}

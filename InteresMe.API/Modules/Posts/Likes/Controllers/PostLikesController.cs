using InteresMe.API.BuildingBlocks.Security;
using InteresMe.API.Modules.Posts.Likes.Contracts.Requests;
using InteresMe.API.Modules.Posts.Likes.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using static InteresMe.API.BuildingBlocks.Results.ApplicationResultMapper;

namespace InteresMe.API.Modules.Posts.Likes.Controllers;

[ApiController]
[Authorize]
[Route("api/posts/{postId}/like")]
public sealed class PostLikesController(
    IPostLikeService postLikeService,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpPost]
    public Task<IActionResult> Toggle(
        Guid postId,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] LikePostRequest? request,
        CancellationToken cancellationToken)
    {
        _ = request;
        return ToActionResult(postLikeService.ToggleAsync(
            currentUser.UserId,
            postId,
            cancellationToken));
    }

    [HttpDelete]
    public Task<IActionResult> Unlike(
        Guid postId,
        CancellationToken cancellationToken) =>
        ToActionResult(postLikeService.UnlikeAsync(
            currentUser.UserId,
            postId,
            cancellationToken));
}

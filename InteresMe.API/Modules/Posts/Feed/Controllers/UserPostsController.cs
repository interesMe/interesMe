using InteresMe.API.BuildingBlocks.Security;
using InteresMe.API.Modules.Posts.Feed.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static InteresMe.API.BuildingBlocks.Results.ApplicationResultMapper;

namespace InteresMe.API.Modules.Posts.Feed.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public sealed class UserPostsController(
    IPostFeedService postFeedService,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpGet("{userId}/posts")]
    public Task<IActionResult> GetUserPosts(
        Guid userId,
        [FromQuery] string? cursor = null,
        [FromQuery] int pageSize = PostFeedRules.DefaultPageSize,
        CancellationToken cancellationToken = default) =>
        ToActionResult(postFeedService.GetUserPostsAsync(
            currentUser.UserId,
            userId,
            cursor,
            pageSize,
            cancellationToken));

    [HttpGet("me/posts")]
    public Task<IActionResult> GetMyPosts(
        [FromQuery] string? cursor = null,
        [FromQuery] int pageSize = PostFeedRules.DefaultPageSize,
        CancellationToken cancellationToken = default) =>
        ToActionResult(postFeedService.GetUserPostsAsync(
            currentUser.UserId,
            currentUser.UserId,
            cursor,
            pageSize,
            cancellationToken));
}

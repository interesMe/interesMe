using InteresMe.API.BuildingBlocks.Security;
using InteresMe.API.Modules.Posts.Feed.Contracts.Requests;
using InteresMe.API.Modules.Posts.Feed.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static InteresMe.API.BuildingBlocks.Results.ApplicationResultMapper;

namespace InteresMe.API.Modules.Posts.Feed.Controllers;

[ApiController]
[Authorize]
[Route("api/posts")]
public sealed class PostsController(
    IPostFeedService postFeedService,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreatePostRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await postFeedService.CreateAsync(
            currentUser.UserId,
            request,
            cancellationToken);
        if (!result.IsSuccess)
        {
            return ToActionResult(result);
        }

        return CreatedAtAction(
            nameof(GetById),
            new { postId = result.Response!.Id },
            result.Response);
    }

    [HttpGet("{postId}")]
    public Task<IActionResult> GetById(
        Guid postId,
        CancellationToken cancellationToken) =>
        ToActionResult(postFeedService.GetByIdAsync(
            currentUser.UserId,
            postId,
            cancellationToken));
}

using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.BuildingBlocks.Security;
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
    IPostCreateRequestReader postCreateRequestReader,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(PostFeedRules.MaxMultipartRequestSizeBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = PostFeedRules.MaxMultipartRequestSizeBytes)]
    public async Task<IActionResult> Create(
        CancellationToken cancellationToken)
    {
        var binding = await postCreateRequestReader.ReadAsync(Request, cancellationToken);
        if (binding.ErrorMessage is not null)
        {
            return ToActionResult(ApplicationResult<object>.Failure(
                ApplicationErrorKind.Validation,
                binding.ErrorCode ?? PostErrorCodes.RequestInvalid,
                binding.ErrorMessage));
        }

        var result = await postFeedService.CreateAsync(
            currentUser.UserId,
            binding.Request,
            binding.Attachments,
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

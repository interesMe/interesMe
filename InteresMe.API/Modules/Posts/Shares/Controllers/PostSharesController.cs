using InteresMe.API.Modules.Posts.Shares.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static InteresMe.API.BuildingBlocks.Results.ApplicationResultMapper;

namespace InteresMe.API.Modules.Posts.Shares.Controllers;

[ApiController]
[Authorize]
[Route("api/posts/{postId}/share")]
public sealed class PostSharesController(IPostShareService postShareService) : ControllerBase
{
    [HttpPost]
    public Task<IActionResult> GetOrCreate(
        Guid postId,
        CancellationToken cancellationToken)
    {
        var publicBaseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";

        return ToActionResult(postShareService.GetOrCreateAsync(
            postId,
            publicBaseUrl,
            cancellationToken));
    }
}

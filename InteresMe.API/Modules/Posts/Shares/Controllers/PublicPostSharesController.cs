using InteresMe.API.Modules.Posts.Shares.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static InteresMe.API.BuildingBlocks.Results.ApplicationResultMapper;

namespace InteresMe.API.Modules.Posts.Shares.Controllers;

[ApiController]
[AllowAnonymous]
[Route("p")]
public sealed class PublicPostSharesController(IPostShareService postShareService) : ControllerBase
{
    [HttpGet("{token}")]
    public Task<IActionResult> GetPublic(
        string token,
        CancellationToken cancellationToken) =>
        ToActionResult(postShareService.GetPublicAsync(token, cancellationToken));
}

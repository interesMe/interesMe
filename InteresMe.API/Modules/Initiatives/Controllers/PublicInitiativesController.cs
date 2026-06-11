using InteresMe.API.Modules.Initiatives.Services.InitiativeQueries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static InteresMe.API.BuildingBlocks.Results.ApplicationResultMapper;

namespace InteresMe.API.Modules.Initiatives.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/public/initiatives")]
public sealed class PublicInitiativesController(
    IInitiativeQueryService initiativeQueryService) : ControllerBase
{
    [HttpGet("{slug}")]
    public Task<IActionResult> GetBySlug(
        string slug,
        CancellationToken cancellationToken) =>
        ToActionResult(initiativeQueryService.GetPublicBySlugAsync(slug, cancellationToken));
}

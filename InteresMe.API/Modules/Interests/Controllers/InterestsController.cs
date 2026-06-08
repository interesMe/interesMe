using InteresMe.API.Modules.Interests.DTOs;
using InteresMe.API.Modules.Interests.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static InteresMe.API.BuildingBlocks.Results.ApplicationResultMapper;

namespace InteresMe.API.Modules.Interests.Controllers;

[ApiController]
[Authorize]
[Route("api/interests")]
public class InterestsController(IInterestService interestService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken) =>
        Ok(await interestService.GetAllAsync(cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateInterestRequest request,
        CancellationToken cancellationToken) =>
        ToActionResult(await interestService.CreateAsync(request, cancellationToken));
}

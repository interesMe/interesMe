using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.BuildingBlocks.Security;
using InteresMe.API.Modules.Initiatives.DTOs;
using InteresMe.API.Modules.Initiatives.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static InteresMe.API.BuildingBlocks.Results.ApplicationResultMapper;

namespace InteresMe.API.Modules.Initiatives.Controllers;

[ApiController]
[Authorize]
[Route("api/initiatives")]
public sealed class InitiativesController(
    IInitiativeService initiativeService,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken) =>
        Ok(await initiativeService.GetAllAsync(cancellationToken));

    [HttpPost]
    public Task<IActionResult> Create(
        [FromBody] CreateInitiativeRequest request,
        CancellationToken cancellationToken) =>
        WithCurrentUserId(
            userId => initiativeService.CreateAsync(userId, request, cancellationToken));

    [HttpGet("{id:guid}")]
    public Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken) =>
        ToActionResult(initiativeService.GetByIdAsync(id, cancellationToken));

    [HttpPut("{id:guid}")]
    public Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateInitiativeRequest request,
        CancellationToken cancellationToken) =>
        WithCurrentUserId(
            userId => initiativeService.UpdateAsync(userId, id, request, cancellationToken));

    [HttpPost("{id:guid}/join-requests")]
    public Task<IActionResult> CreateJoinRequest(
        Guid id,
        [FromBody] CreateInitiativeJoinRequestRequest request,
        CancellationToken cancellationToken) =>
        WithCurrentUserId(
            userId => initiativeService.CreateJoinRequestAsync(
                userId,
                id,
                request,
                cancellationToken));

    [HttpGet("{id:guid}/join-requests")]
    public Task<IActionResult> GetJoinRequests(
        Guid id,
        CancellationToken cancellationToken) =>
        WithCurrentUserId(
            userId => initiativeService.GetJoinRequestsAsync(
                userId,
                id,
                cancellationToken));

    [HttpPost("{id:guid}/join-requests/{requestId:guid}/accept")]
    public Task<IActionResult> AcceptJoinRequest(
        Guid id,
        Guid requestId,
        CancellationToken cancellationToken) =>
        WithCurrentUserId(
            userId => initiativeService.AcceptJoinRequestAsync(
                userId,
                id,
                requestId,
                cancellationToken));

    [HttpPost("{id:guid}/join-requests/{requestId:guid}/reject")]
    public Task<IActionResult> RejectJoinRequest(
        Guid id,
        Guid requestId,
        CancellationToken cancellationToken) =>
        WithCurrentUserId(
            userId => initiativeService.RejectJoinRequestAsync(
                userId,
                id,
                requestId,
                cancellationToken));

    private async Task<IActionResult> WithCurrentUserId<T>(
        Func<Guid, Task<ApplicationResult<T>>> action)
    {
        Guid userId;

        try
        {
            userId = currentUser.UserId;
        }
        catch (InvalidOperationException)
        {
            return Unauthorized(new { message = "Invalid access token." });
        }

        return ToActionResult(await action(userId));
    }
}

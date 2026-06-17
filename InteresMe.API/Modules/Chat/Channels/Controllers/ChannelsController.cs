using InteresMe.API.BuildingBlocks.Security;
using InteresMe.API.Modules.Chat.Channels.DTOs;
using InteresMe.API.Modules.Chat.Channels.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static InteresMe.API.BuildingBlocks.Results.ApplicationResultMapper;

namespace InteresMe.API.Modules.Chat.Channels.Controllers;

[ApiController]
[Authorize]
[Route("api/chats/channels")]
public sealed class ChannelsController(
    IChannelService channelService,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public Task<IActionResult> GetChannels(
        CancellationToken cancellationToken) =>
        WithCurrentUserId(
            async userId => Ok(await channelService.GetChannelsAsync(userId, cancellationToken)));

    [HttpPost]
    public Task<IActionResult> CreateChannel(
        [FromBody] CreateChannelRequest request,
        CancellationToken cancellationToken) =>
        WithCurrentUserId(
            userId => ToActionResult(channelService.CreateChannelAsync(
                userId,
                request,
                cancellationToken)));

    [HttpGet("{channelId:guid}")]
    public Task<IActionResult> GetChannel(
        Guid channelId,
        CancellationToken cancellationToken) =>
        WithCurrentUserId(
            userId => ToActionResult(channelService.GetChannelAsync(
                userId,
                channelId,
                cancellationToken)));

    [HttpPost("{channelId:guid}/members/{userId:guid}")]
    public Task<IActionResult> AddMember(
        Guid channelId,
        Guid userId,
        CancellationToken cancellationToken) =>
        WithCurrentUserId(
            currentUserId => ToActionResult(channelService.AddMemberAsync(
                currentUserId,
                channelId,
                userId,
                cancellationToken)));

    private async Task<IActionResult> WithCurrentUserId(
        Func<Guid, Task<IActionResult>> action)
    {
        try
        {
            return await action(currentUser.UserId);
        }
        catch (InvalidOperationException)
        {
            return Unauthorized(new { message = "Invalid access token." });
        }
    }
}

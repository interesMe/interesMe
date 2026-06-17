using InteresMe.API.BuildingBlocks.Security;
using InteresMe.API.Modules.Chat.Groups.Services;
using InteresMe.API.Modules.Chat.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static InteresMe.API.BuildingBlocks.Results.ApplicationResultMapper;

namespace InteresMe.API.Modules.Chat.Groups.Controllers;

[ApiController]
[Authorize]
[Route("api/chats/groups")]
public sealed class GroupChatsController(
    IGroupChatService groupChatService,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public Task<IActionResult> GetGroupChats(
        CancellationToken cancellationToken) =>
        WithCurrentUserId(
            async userId => Ok(await groupChatService.GetGroupChatsAsync(userId, cancellationToken)));

    [HttpPost("initiative/{initiativeId:guid}")]
    public Task<IActionResult> GetOrCreateInitiativeGroupChat(
        Guid initiativeId,
        CancellationToken cancellationToken) =>
        WithCurrentUserId(
            userId => ToActionResult(groupChatService.GetOrCreateInitiativeGroupChatAsync(
                userId,
                initiativeId,
                cancellationToken)));

    [HttpGet("{groupChatId:guid}")]
    public Task<IActionResult> GetGroupChat(
        Guid groupChatId,
        CancellationToken cancellationToken) =>
        WithCurrentUserId(
            userId => ToActionResult(groupChatService.GetGroupChatAsync(
                userId,
                groupChatId,
                cancellationToken)));

    [HttpGet("{groupChatId:guid}/messages")]
    public Task<IActionResult> GetMessages(
        Guid groupChatId,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default) =>
        WithCurrentUserId(
            userId => ToActionResult(groupChatService.GetMessagesAsync(
                userId,
                groupChatId,
                take,
                cancellationToken)));

    [HttpPost("{groupChatId:guid}/messages")]
    public Task<IActionResult> SendMessage(
        Guid groupChatId,
        [FromBody] SendMessageRequest request,
        CancellationToken cancellationToken) =>
        WithCurrentUserId(
            userId => ToActionResult(groupChatService.SendMessageAsync(
                userId,
                groupChatId,
                request,
                cancellationToken)));

    [HttpPost("{groupChatId:guid}/participants/{userId:guid}")]
    public Task<IActionResult> AddParticipant(
        Guid groupChatId,
        Guid userId,
        CancellationToken cancellationToken) =>
        WithCurrentUserId(
            currentUserId => ToActionResult(groupChatService.AddParticipantAsync(
                currentUserId,
                groupChatId,
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

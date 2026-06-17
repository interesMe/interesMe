using InteresMe.API.BuildingBlocks.Security;
using InteresMe.API.Modules.Chat.DirectMessages.Services;
using InteresMe.API.Modules.Chat.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static InteresMe.API.BuildingBlocks.Results.ApplicationResultMapper;

namespace InteresMe.API.Modules.Chat.DirectMessages.Controllers;

[ApiController]
[Authorize]
[Route("api/chats/direct")]
public sealed class DirectMessagesController(
    IDirectMessageService directMessageService,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public Task<IActionResult> GetConversations(
        CancellationToken cancellationToken) =>
        WithCurrentUserId(
            async userId => Ok(await directMessageService.GetConversationsAsync(userId, cancellationToken)));

    [HttpPost("{userId:guid}")]
    public Task<IActionResult> GetOrCreateConversation(
        Guid userId,
        CancellationToken cancellationToken) =>
        WithCurrentUserId(
            currentUserId => ToActionResult(directMessageService.GetOrCreateConversationAsync(
                currentUserId,
                userId,
                cancellationToken)));

    [HttpGet("{conversationId:guid}/messages")]
    public Task<IActionResult> GetMessages(
        Guid conversationId,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default) =>
        WithCurrentUserId(
            userId => ToActionResult(directMessageService.GetMessagesAsync(
                userId,
                conversationId,
                take,
                cancellationToken)));

    [HttpPost("{conversationId:guid}/messages")]
    public Task<IActionResult> SendMessage(
        Guid conversationId,
        [FromBody] SendMessageRequest request,
        CancellationToken cancellationToken) =>
        WithCurrentUserId(
            userId => ToActionResult(directMessageService.SendMessageAsync(
                userId,
                conversationId,
                request,
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

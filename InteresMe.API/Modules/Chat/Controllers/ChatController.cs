using InteresMe.API.BuildingBlocks.Security;
using InteresMe.API.Modules.Chat.DTOs;
using InteresMe.API.Modules.Chat.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static InteresMe.API.BuildingBlocks.Results.ApplicationResultMapper;

namespace InteresMe.API.Modules.Chat.Controllers;

[ApiController]
[Authorize]
[Route("api/chats")]
public sealed class ChatController(
    IChatService chatService,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public Task<IActionResult> GetChats(
        CancellationToken cancellationToken) =>
        WithCurrentUserId(
            async userId => Ok(await chatService.GetChatsAsync(userId, cancellationToken)));

    [HttpGet("{chatId:guid}")]
    public Task<IActionResult> GetChat(
        Guid chatId,
        CancellationToken cancellationToken) =>
        WithCurrentUserId(
            userId => ToActionResult(chatService.GetChatAsync(userId, chatId, cancellationToken)));

    [HttpGet("{chatId:guid}/messages")]
    public Task<IActionResult> GetMessages(
        Guid chatId,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default) =>
        WithCurrentUserId(
            userId => ToActionResult(chatService.GetMessagesAsync(
                userId,
                chatId,
                take,
                cancellationToken)));

    [HttpPost("{chatId:guid}/messages")]
    public Task<IActionResult> SendMessage(
        Guid chatId,
        [FromBody] SendMessageRequest request,
        CancellationToken cancellationToken) =>
        WithCurrentUserId(
            userId => ToActionResult(chatService.SendMessageAsync(
                userId,
                chatId,
                request,
                cancellationToken)));

    [HttpPost("initiative/{initiativeId:guid}")]
    public Task<IActionResult> GetOrCreateInitiativeChat(
        Guid initiativeId,
        CancellationToken cancellationToken) =>
        WithCurrentUserId(
            userId => ToActionResult(chatService.GetOrCreateInitiativeChatAsync(
                userId,
                initiativeId,
                cancellationToken)));

    [HttpPost("{chatId:guid}/participants/{userId:guid}")]
    public Task<IActionResult> AddParticipant(
        Guid chatId,
        Guid userId,
        CancellationToken cancellationToken) =>
        WithCurrentUserId(
            currentUserId => ToActionResult(chatService.AddParticipantAsync(
                currentUserId,
                chatId,
                userId,
                cancellationToken)));

    private async Task<IActionResult> WithCurrentUserId(
        Func<Guid, Task<IActionResult>> action)
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

        return await action(userId);
    }
}

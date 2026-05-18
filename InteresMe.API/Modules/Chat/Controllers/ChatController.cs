using InteresMe.API.Modules.Chat.DTOs;
using InteresMe.API.Modules.Chat.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InteresMe.API.Modules.Chat.Controllers;

[ApiController]
[Authorize]
[Route("api/chat")]
public class ChatController(IChatService chatService) : ControllerBase
{
    [HttpGet("conversation")]
    public async Task<ActionResult<IReadOnlyList<MessageResponse>>> GetConversation(
        [FromQuery] Guid userId,
        [FromQuery] Guid otherUserId)
    {
        if (userId == Guid.Empty || otherUserId == Guid.Empty)
        {
            return BadRequest(new { message = "Both userId and otherUserId are required." });
        }

        var messages = await chatService.GetConversationAsync(userId, otherUserId);
        return Ok(messages);
    }

    [HttpPost("messages")]
    public async Task<ActionResult<MessageResponse>> Send([FromBody] SendMessageRequest request)
    {
        var message = await chatService.SendAsync(request);
        if (message is null)
        {
            return BadRequest(new { message = "Could not send message. Check users and content." });
        }

        return Ok(message);
    }
}

using InteresMe.API.Modules.Feed.DTOs;
using InteresMe.API.Modules.Feed.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InteresMe.API.Modules.Feed.Controllers;

[ApiController]
[Authorize]
[Route("api/feed")]
public class FeedController(IFeedService feedService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PostResponse>>> GetFeed()
    {
        var posts = await feedService.GetFeedAsync();
        return Ok(posts);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PostResponse>> GetById(Guid id)
    {
        var post = await feedService.GetByIdAsync(id);
        if (post is null)
        {
            return NotFound(new { message = "Post not found." });
        }

        return Ok(post);
    }

    [HttpPost]
    public async Task<ActionResult<PostResponse>> Create([FromBody] CreatePostRequest request)
    {
        var post = await feedService.CreateAsync(request);
        if (post is null)
        {
            return BadRequest(new { message = "Could not create post. Check author and content." });
        }

        return CreatedAtAction(nameof(GetById), new { id = post.Id }, post);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await feedService.DeleteAsync(id);
        if (!deleted)
        {
            return NotFound(new { message = "Post not found." });
        }

        return NoContent();
    }
}

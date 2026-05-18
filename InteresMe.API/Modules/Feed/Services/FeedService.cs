using System.Collections.Concurrent;
using InteresMe.API.Modules.Feed.DTOs;
using InteresMe.API.Modules.Feed.Models;
using InteresMe.API.Modules.Users.Services;

namespace InteresMe.API.Modules.Feed.Services;

public class FeedService(IUserService userService) : IFeedService
{
    private readonly ConcurrentDictionary<Guid, Post> _posts = new();

    public Task<IReadOnlyList<PostResponse>> GetFeedAsync()
    {
        var posts = _posts.Values
            .OrderByDescending(post => post.CreatedAt)
            .Select(ToResponse)
            .ToList();

        return Task.FromResult<IReadOnlyList<PostResponse>>(posts);
    }

    public Task<PostResponse?> GetByIdAsync(Guid id)
    {
        _posts.TryGetValue(id, out var post);
        return Task.FromResult(post is null ? null : ToResponse(post));
    }

    public async Task<PostResponse?> CreateAsync(CreatePostRequest request)
    {
        if (request.AuthorId == Guid.Empty || string.IsNullOrWhiteSpace(request.Content))
        {
            return null;
        }

        var author = await userService.GetByIdAsync(request.AuthorId);
        if (author is null)
        {
            return null;
        }

        var post = new Post
        {
            Id = Guid.NewGuid(),
            AuthorId = request.AuthorId,
            Content = request.Content.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _posts[post.Id] = post;
        return ToResponse(post);
    }

    public Task<bool> DeleteAsync(Guid id) => Task.FromResult(_posts.TryRemove(id, out _));

    private static PostResponse ToResponse(Post post) => new()
    {
        Id = post.Id,
        AuthorId = post.AuthorId,
        Content = post.Content,
        CreatedAt = post.CreatedAt
    };
}

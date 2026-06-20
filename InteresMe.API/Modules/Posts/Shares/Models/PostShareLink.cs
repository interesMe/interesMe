using InteresMe.API.Modules.Posts.Feed.Models;

namespace InteresMe.API.Modules.Posts.Shares.Models;

public sealed class PostShareLink
{
    public Guid PostId { get; set; }

    public string Token { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public Post Post { get; set; } = null!;
}

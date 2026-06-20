using InteresMe.API.Modules.Auth.Models;
using InteresMe.API.Modules.Posts.Feed.Models;

namespace InteresMe.API.Modules.Posts.Likes.Models;

public sealed class PostLike
{
    public Guid PostId { get; set; }

    public Guid UserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public Post Post { get; set; } = null!;

    public User User { get; set; } = null!;
}

using InteresMe.API.Modules.Auth.Models;
using InteresMe.API.Modules.Posts.Feed.Models;

namespace InteresMe.API.Modules.Posts.Comments.Models;

public sealed class Comment
{
    public Guid Id { get; set; }

    public Guid PostId { get; set; }

    public Guid AuthorId { get; set; }

    public string Body { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public Post Post { get; set; } = null!;

    public User Author { get; set; } = null!;
}

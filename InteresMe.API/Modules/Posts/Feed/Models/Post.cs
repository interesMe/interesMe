using InteresMe.API.Modules.Auth.Models;
using InteresMe.API.Modules.Initiatives.Domain.Entities;

namespace InteresMe.API.Modules.Posts.Feed.Models;

public sealed class Post
{
    public Guid Id { get; set; }

    public Guid AuthorId { get; set; }

    public string Body { get; set; } = string.Empty;

    public Guid? InitiativeId { get; set; }

    public DateTime CreatedAt { get; set; }

    public User Author { get; set; } = null!;

    public Initiative? Initiative { get; set; }

    public ICollection<PostAttachment> Attachments { get; set; } = [];
}

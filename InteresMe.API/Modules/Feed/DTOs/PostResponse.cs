namespace InteresMe.API.Modules.Feed.DTOs;

public class PostResponse
{
    public Guid Id { get; set; }

    public Guid AuthorId { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}

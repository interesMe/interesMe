namespace InteresMe.API.Modules.Feed.DTOs;

public class CreatePostRequest
{
    public Guid AuthorId { get; set; }

    public string Content { get; set; } = string.Empty;
}

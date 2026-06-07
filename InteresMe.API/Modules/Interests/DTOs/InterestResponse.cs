namespace InteresMe.API.Modules.Interests.DTOs;

public class InterestResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}

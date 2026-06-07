namespace InteresMe.API.Modules.Interests.DTOs;

public class CreateInterestRequest
{
    public string Name { get; set; } = string.Empty;

    public string? Slug { get; set; }
}

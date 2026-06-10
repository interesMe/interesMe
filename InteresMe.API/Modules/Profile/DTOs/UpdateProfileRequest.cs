namespace InteresMe.API.Modules.Profile.DTOs;

public class UpdateProfileRequest
{
    public string DisplayName { get; set; } = string.Empty;

    public string? Headline { get; set; }

    public string? Bio { get; set; }

    public string? City { get; set; }

    public string? AvatarUrl { get; set; }

    public DateOnly? BirthDate { get; set; }
}

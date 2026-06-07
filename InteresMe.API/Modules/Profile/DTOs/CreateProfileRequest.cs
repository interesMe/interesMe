namespace InteresMe.API.Modules.Profile.DTOs;

public class CreateProfileRequest
{
    public string DisplayName { get; set; } = string.Empty;

    public string? City { get; set; }

    public string? AvatarUrl { get; set; }

    public DateOnly? BirthDate { get; set; }
}

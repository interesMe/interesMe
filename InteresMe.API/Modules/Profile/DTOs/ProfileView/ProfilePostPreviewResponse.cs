namespace InteresMe.API.Modules.Profile.DTOs.ProfileView;

public sealed class ProfilePostPreviewResponse
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}

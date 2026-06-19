namespace InteresMe.API.Modules.Profile.DTOs.ProfileView;

public sealed class ProfileInterestCategoryResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public List<ProfileInterestResponse> Interests { get; set; } = [];
}

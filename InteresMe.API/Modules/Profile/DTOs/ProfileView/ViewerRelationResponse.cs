namespace InteresMe.API.Modules.Profile.DTOs.ProfileView;

public sealed class ViewerRelationResponse
{
    public bool IsFollowing { get; set; }

    public bool CanFollow { get; set; }

    public bool CanMessage { get; set; }
}

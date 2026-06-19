namespace InteresMe.API.Modules.Profile.DTOs.ProfileView;

public sealed class ProfileViewResponse
{
    public BasicProfileResponse BasicProfile { get; set; } = new();

    public ProfileStatsResponse Stats { get; set; } = new();

    public ViewerRelationResponse ViewerRelation { get; set; } = new();

    public List<ProfileInterestCategoryResponse> Interests { get; set; } = [];

    public List<ProfileInitiativePreviewResponse> CreatedInitiatives { get; set; } = [];

    public List<ProfileInitiativePreviewResponse> JoinedInitiatives { get; set; } = [];

    public List<ProfileHistoryPreviewResponse> RecentHistory { get; set; } = [];

    public List<ProfilePostPreviewResponse> RecentPosts { get; set; } = [];
}

namespace InteresMe.API.Modules.Profile.DTOs.ProfileView;

public sealed class ProfileStatsResponse
{
    public int InterestsCount { get; set; }

    public int SharedInterestsCount { get; set; }

    public int CreatedInitiativesCount { get; set; }

    public int JoinedInitiativesCount { get; set; }

    public int FollowersCount { get; set; }

    public int FollowingCount { get; set; }

    public int HistoryEventsCount { get; set; }

    public int PostsCount { get; set; }
}

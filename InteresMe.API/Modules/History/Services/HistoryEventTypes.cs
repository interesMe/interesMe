namespace InteresMe.API.Modules.History.Services;

public static class HistoryEventTypes
{
    public const string CreatedInitiative = "created_initiative";
    public const string JoinedInitiative = "joined_initiative";
    public const string CompletedInitiative = "completed_initiative";
    public const string AddedInterest = "added_interest";
    public const string JoinedCommunity = "joined_community";
    public const string OrganizedEvent = "organized_event";
    public const string CreatedPost = "created_post";
    public const string AchievementUnlocked = "achievement_unlocked";
    public const string ReceivedInvitation = "received_invitation";

    public static bool IsSupported(string type) =>
        type is CreatedInitiative
            or JoinedInitiative
            or CompletedInitiative
            or AddedInterest
            or JoinedCommunity
            or OrganizedEvent
            or CreatedPost
            or AchievementUnlocked
            or ReceivedInvitation;
}

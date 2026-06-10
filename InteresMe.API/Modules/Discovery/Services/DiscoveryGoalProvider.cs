using InteresMe.API.Modules.Discovery.DTOs;
using InteresMe.API.Modules.Discovery.Models;

namespace InteresMe.API.Modules.Discovery.Services;

internal static class DiscoveryGoalProvider
{
    private static readonly IReadOnlyList<DiscoveryGoalResponse> Goals =
    [
        new()
        {
            Goal = UserGoal.Connect,
            Key = "connect",
            Title = "Connect",
            Description = "Friendships, relationships, and like-minded people."
        },
        new()
        {
            Goal = UserGoal.Learn,
            Key = "learn",
            Title = "Learn",
            Description = "University help, mentoring, and learning."
        },
        new()
        {
            Goal = UserGoal.Build,
            Key = "build",
            Title = "Build",
            Description = "Startups, projects, teams, and co-founders."
        },
        new()
        {
            Goal = UserGoal.Play,
            Key = "play",
            Title = "Play",
            Description = "Games, activities, and entertainment."
        },
        new()
        {
            Goal = UserGoal.Explore,
            Key = "explore",
            Title = "Explore",
            Description = "Discovery mode when you are not sure yet."
        }
    ];

    public static IReadOnlyList<DiscoveryGoalResponse> GetGoals() => Goals;
}

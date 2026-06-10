using InteresMe.API.Modules.Discovery.Models;

namespace InteresMe.API.Modules.Discovery.DTOs;

public sealed class DiscoveryGoalResponse
{
    public UserGoal Goal { get; set; }

    public string Key { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}

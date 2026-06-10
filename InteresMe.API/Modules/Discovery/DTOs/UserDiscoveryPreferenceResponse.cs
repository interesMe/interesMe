using InteresMe.API.Modules.Discovery.Models;

namespace InteresMe.API.Modules.Discovery.DTOs;

public sealed class UserDiscoveryPreferenceResponse
{
    public UserGoal? Goal { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

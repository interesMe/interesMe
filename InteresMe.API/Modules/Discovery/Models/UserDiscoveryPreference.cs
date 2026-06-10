using InteresMe.API.Modules.Auth.Models;

namespace InteresMe.API.Modules.Discovery.Models;

public sealed class UserDiscoveryPreference
{
    public Guid UserId { get; set; }

    public UserGoal Goal { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public User User { get; set; } = null!;
}

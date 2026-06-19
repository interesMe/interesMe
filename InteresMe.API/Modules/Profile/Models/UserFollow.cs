using InteresMe.API.Modules.Auth.Models;

namespace InteresMe.API.Modules.Profile.Models;

public sealed class UserFollow
{
    public Guid FollowerId { get; set; }

    public Guid FollowedId { get; set; }

    public DateTime CreatedAt { get; set; }

    public User Follower { get; set; } = null!;

    public User Followed { get; set; } = null!;
}

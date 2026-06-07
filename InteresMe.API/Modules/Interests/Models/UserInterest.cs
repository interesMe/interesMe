using InteresMe.API.Modules.Auth.Models;

namespace InteresMe.API.Modules.Interests.Models;

public sealed class UserInterest
{
    public Guid UserId { get; set; }

    public Guid InterestId { get; set; }

    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;

    public Interest Interest { get; set; } = null!;
}

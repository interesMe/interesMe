using InteresMe.API.Modules.Auth.Models;

namespace InteresMe.API.Modules.Interests.Models;

public sealed class UserSubinterest
{
    public Guid UserId { get; set; }

    public Guid SubinterestId { get; set; }

    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;

    public Subinterest Subinterest { get; set; } = null!;
}

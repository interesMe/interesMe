using InteresMe.API.Modules.Auth.Models;

namespace InteresMe.API.Modules.Verification.Models;

public sealed class UserVerification
{
    public Guid UserId { get; set; }

    public DateTime? EmailVerifiedAt { get; set; }

    public DateTime? PhoneVerifiedAt { get; set; }

    public DateTime? IdentityVerifiedAt { get; set; }

    public int TrustScore { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public User User { get; set; } = null!;
}

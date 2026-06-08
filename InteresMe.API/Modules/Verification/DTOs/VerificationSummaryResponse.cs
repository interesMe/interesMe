namespace InteresMe.API.Modules.Verification.DTOs;

public sealed class VerificationSummaryResponse
{
    public Guid UserId { get; set; }

    public bool EmailVerified { get; set; }

    public bool PhoneVerified { get; set; }

    public bool IdentityVerified { get; set; }

    public DateTime? EmailVerifiedAt { get; set; }

    public DateTime? PhoneVerifiedAt { get; set; }

    public DateTime? IdentityVerifiedAt { get; set; }

    public int TrustScore { get; set; }
}

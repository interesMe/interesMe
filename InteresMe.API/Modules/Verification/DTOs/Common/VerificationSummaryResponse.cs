using System.Text.Json.Serialization;

namespace InteresMe.API.Modules.Verification.DTOs.Common;

public sealed class VerificationSummaryResponse
{
    public Guid UserId { get; set; }

    public bool EmailVerified { get; set; }

    public bool PhoneVerified { get; set; }

    public bool IdentityVerified { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PhoneNumber { get; set; }

    public DateTime? EmailVerifiedAt { get; set; }

    public DateTime? PhoneVerifiedAt { get; set; }

    public DateTime? IdentityVerifiedAt { get; set; }

    public int TrustScore { get; set; }
}

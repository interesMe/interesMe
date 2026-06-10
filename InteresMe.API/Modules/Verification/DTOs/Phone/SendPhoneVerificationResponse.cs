using System.Text.Json.Serialization;

namespace InteresMe.API.Modules.Verification.DTOs.Phone;

public sealed class SendPhoneVerificationResponse
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Token { get; set; }

    public DateTime ExpiresAt { get; set; }
}

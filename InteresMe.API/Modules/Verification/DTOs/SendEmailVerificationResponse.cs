using System.Text.Json.Serialization;

namespace InteresMe.API.Modules.Verification.DTOs;

public sealed class SendEmailVerificationResponse
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Token { get; set; }

    public DateTime ExpiresAt { get; set; }
}

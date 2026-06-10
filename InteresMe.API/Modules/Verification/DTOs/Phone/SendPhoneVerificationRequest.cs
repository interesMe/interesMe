namespace InteresMe.API.Modules.Verification.DTOs.Phone;

public sealed class SendPhoneVerificationRequest
{
    public string PhoneNumber { get; set; } = string.Empty;
}

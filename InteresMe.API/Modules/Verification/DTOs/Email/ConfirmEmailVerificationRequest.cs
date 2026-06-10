namespace InteresMe.API.Modules.Verification.DTOs.Email;

public sealed class ConfirmEmailVerificationRequest
{
    public string Token { get; set; } = string.Empty;
}

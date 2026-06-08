namespace InteresMe.API.Modules.Verification.DTOs;

public sealed class ConfirmEmailVerificationRequest
{
    public string Token { get; set; } = string.Empty;
}

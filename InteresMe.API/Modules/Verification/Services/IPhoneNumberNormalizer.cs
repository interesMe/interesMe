namespace InteresMe.API.Modules.Verification.Services;

public interface IPhoneNumberNormalizer
{
    string? Normalize(string? phoneNumber);
}

namespace InteresMe.API.Modules.Verification.Services;

public sealed class PhoneNumberNormalizer : IPhoneNumberNormalizer
{
    public string? Normalize(string? phoneNumber)
    {
        var value = phoneNumber?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = new List<char>(value.Length);

        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];

            if (char.IsDigit(character))
            {
                normalized.Add(character);
                continue;
            }

            if (character == '+' && index == 0)
            {
                normalized.Add(character);
                continue;
            }

            if (character is ' ' or '-' or '(' or ')')
            {
                continue;
            }

            return null;
        }

        var result = new string(normalized.ToArray());
        var digitCount = result.Count(char.IsDigit);

        if (digitCount is < 7 or > 15)
        {
            return null;
        }

        return result;
    }
}

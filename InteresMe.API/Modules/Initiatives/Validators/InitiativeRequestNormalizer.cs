using InteresMe.API.Modules.Initiatives.Contracts.Requests;

namespace InteresMe.API.Modules.Initiatives.Validators;

public static class InitiativeRequestNormalizer
{
    public static NormalizedInitiativeRequest Normalize(
        CreateInitiativeRequest request) => new(
        request.Title?.Trim() ?? string.Empty,
        request.ShortDescription?.Trim() ?? string.Empty,
        request.GoalType,
        NormalizeInterestIds(request.InterestIds),
        NormalizeRoles(request.Roles),
        NormalizeOptional(request.University),
        request.TeamSize,
        request.Status,
        request.Visibility);

    public static NormalizedInitiativeRequest Normalize(
        UpdateInitiativeRequest request) => new(
        request.Title?.Trim() ?? string.Empty,
        request.ShortDescription?.Trim() ?? string.Empty,
        request.GoalType,
        NormalizeInterestIds(request.InterestIds),
        NormalizeRoles(request.Roles),
        NormalizeOptional(request.University),
        request.TeamSize,
        request.Status,
        request.Visibility);

    public static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();

        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static List<Guid> NormalizeInterestIds(List<Guid>? interestIds) =>
        interestIds?.Distinct().ToList() ?? [];

    private static List<string> NormalizeRoles(List<string>? roles) =>
        roles?
            .Select(role => role.Trim())
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];
}

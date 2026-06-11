using InteresMe.API.Data;
using Microsoft.EntityFrameworkCore;

namespace InteresMe.API.Modules.Initiatives.Validators;

public sealed class InitiativeRequestValidator(AppDbContext dbContext) : IInitiativeRequestValidator
{
    private const int TitleMaxLength = 120;
    private const int ShortDescriptionMaxLength = 500;
    private const int UniversityMaxLength = 160;
    private const int RoleNameMaxLength = 80;

    public async Task<string?> ValidateAsync(
        NormalizedInitiativeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return "Title is required.";
        }

        if (request.Title.Length > TitleMaxLength)
        {
            return $"Title must be at most {TitleMaxLength} characters.";
        }

        if (string.IsNullOrWhiteSpace(request.ShortDescription))
        {
            return "Short description is required.";
        }

        if (request.ShortDescription.Length > ShortDescriptionMaxLength)
        {
            return $"Short description must be at most {ShortDescriptionMaxLength} characters.";
        }

        if (!Enum.IsDefined(request.GoalType))
        {
            return "Valid goal type is required.";
        }

        if (request.Status is not null &&
            !Enum.IsDefined(request.Status.Value))
        {
            return "Valid initiative status is required.";
        }

        if (request.Visibility is not null &&
            !Enum.IsDefined(request.Visibility.Value))
        {
            return "Valid initiative visibility is required.";
        }

        if (!string.IsNullOrWhiteSpace(request.University) &&
            request.University.Length > UniversityMaxLength)
        {
            return $"University must be at most {UniversityMaxLength} characters.";
        }

        if (request.TeamSize is not null &&
            request.TeamSize <= 0)
        {
            return "Team size must be greater than zero.";
        }

        if (request.InterestIds.Count == 0)
        {
            return "At least one interest is required.";
        }

        if (request.Roles.Count == 0)
        {
            return "At least one role is required.";
        }

        if (request.Roles.Any(role => role.Length > RoleNameMaxLength))
        {
            return $"Role name must be at most {RoleNameMaxLength} characters.";
        }

        var existingInterestCount = await dbContext.Interests
            .CountAsync(
                interest => request.InterestIds.Contains(interest.Id),
                cancellationToken);

        return existingInterestCount == request.InterestIds.Count
            ? null
            : "One or more interests do not exist.";
    }
}

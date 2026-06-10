using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.Data;
using InteresMe.API.Modules.Discovery.DTOs;
using InteresMe.API.Modules.Discovery.Models;
using Microsoft.EntityFrameworkCore;

namespace InteresMe.API.Modules.Discovery.Services;

public sealed class DiscoveryService(AppDbContext dbContext) : IDiscoveryService
{
    private static readonly IReadOnlyList<DiscoveryGoalResponse> Goals =
    [
        new()
        {
            Goal = UserGoal.Connect,
            Key = "connect",
            Title = "Connect",
            Description = "Friendships, relationships, and like-minded people."
        },
        new()
        {
            Goal = UserGoal.Learn,
            Key = "learn",
            Title = "Learn",
            Description = "University help, mentoring, and learning."
        },
        new()
        {
            Goal = UserGoal.Build,
            Key = "build",
            Title = "Build",
            Description = "Startups, projects, teams, and co-founders."
        },
        new()
        {
            Goal = UserGoal.Play,
            Key = "play",
            Title = "Play",
            Description = "Games, activities, and entertainment."
        },
        new()
        {
            Goal = UserGoal.Explore,
            Key = "explore",
            Title = "Explore",
            Description = "Discovery mode when you are not sure yet."
        }
    ];

    public IReadOnlyList<DiscoveryGoalResponse> GetGoals() => Goals;

    public async Task<ApplicationResult<UserDiscoveryPreferenceResponse>> GetMyPreferenceAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var userExists = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(user => user.Id == userId, cancellationToken);

        if (!userExists)
        {
            return ApplicationResult<UserDiscoveryPreferenceResponse>.Failure(
                ApplicationErrorKind.NotFound,
                "User was not found.");
        }

        var preference = await dbContext.UserDiscoveryPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(
                userPreference => userPreference.UserId == userId,
                cancellationToken);

        return ApplicationResult<UserDiscoveryPreferenceResponse>.Success(
            ToResponse(preference));
    }

    public async Task<ApplicationResult<UserDiscoveryPreferenceResponse>> UpdateMyGoalAsync(
        Guid userId,
        UpdateUserGoalRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(request.Goal))
        {
            return ApplicationResult<UserDiscoveryPreferenceResponse>.Failure(
                ApplicationErrorKind.Validation,
                "Valid discovery goal is required.");
        }

        var userExists = await dbContext.Users
            .AnyAsync(user => user.Id == userId, cancellationToken);

        if (!userExists)
        {
            return ApplicationResult<UserDiscoveryPreferenceResponse>.Failure(
                ApplicationErrorKind.NotFound,
                "User was not found.");
        }

        var now = DateTime.UtcNow;
        var preference = await dbContext.UserDiscoveryPreferences
            .FirstOrDefaultAsync(
                userPreference => userPreference.UserId == userId,
                cancellationToken);

        if (preference is null)
        {
            preference = new UserDiscoveryPreference
            {
                UserId = userId,
                Goal = request.Goal,
                CreatedAt = now,
                UpdatedAt = now
            };

            dbContext.UserDiscoveryPreferences.Add(preference);
        }
        else
        {
            preference.Goal = request.Goal;
            preference.UpdatedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return ApplicationResult<UserDiscoveryPreferenceResponse>.Success(
            ToResponse(preference));
    }

    private static UserDiscoveryPreferenceResponse ToResponse(
        UserDiscoveryPreference? preference) => new()
    {
        Goal = preference?.Goal,
        UpdatedAt = preference?.UpdatedAt
    };
}

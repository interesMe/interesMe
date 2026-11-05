using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.Data;
using InteresMe.API.Modules.Discovery.DTOs;
using Microsoft.EntityFrameworkCore;

namespace InteresMe.API.Modules.Discovery.Services;

/// <summary>
/// Finds people who share interests with the viewer (or with an explicit interest selection).
/// Results are ranked by the number of shared interests, then by display name.
/// </summary>
public sealed class PeopleSearchService(AppDbContext dbContext) : IPeopleSearchService
{
    internal const int DefaultLimit = 20;
    internal const int MaxLimit = 50;
    internal const int MaxInterestIds = 20;

    public async Task<ApplicationResult<PeopleSearchResponse>> SearchByInterestsAsync(
        Guid viewerUserId,
        IReadOnlyCollection<Guid>? interestIds,
        int? limit,
        CancellationToken cancellationToken = default)
    {
        var take = limit ?? DefaultLimit;

        if (take < 1 || take > MaxLimit)
        {
            return ApplicationResult<PeopleSearchResponse>.Failure(
                ApplicationErrorKind.Validation,
                $"Limit must be between 1 and {MaxLimit}.");
        }

        var requestedIds = (interestIds ?? [])
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (requestedIds.Count > MaxInterestIds)
        {
            return ApplicationResult<PeopleSearchResponse>.Failure(
                ApplicationErrorKind.Validation,
                $"At most {MaxInterestIds} interests can be used in one search.");
        }

        var usedMyInterests = requestedIds.Count == 0;
        var searchIds = usedMyInterests
            ? await dbContext.UserInterests
                .AsNoTracking()
                .Where(userInterest => userInterest.UserId == viewerUserId)
                .Select(userInterest => userInterest.InterestId)
                .ToListAsync(cancellationToken)
            : await dbContext.Interests
                .AsNoTracking()
                .Where(interest => requestedIds.Contains(interest.Id) && interest.IsActive)
                .Select(interest => interest.Id)
                .ToListAsync(cancellationToken);

        if (!usedMyInterests && searchIds.Count != requestedIds.Count)
        {
            return ApplicationResult<PeopleSearchResponse>.Failure(
                ApplicationErrorKind.Validation,
                "One or more interests do not exist.");
        }

        if (searchIds.Count == 0)
        {
            return ApplicationResult<PeopleSearchResponse>.Success(new PeopleSearchResponse
            {
                InterestIds = [],
                UsedMyInterests = usedMyInterests,
                Items = []
            });
        }

        var ranked = await dbContext.UserInterests
            .AsNoTracking()
            .Where(userInterest =>
                searchIds.Contains(userInterest.InterestId) &&
                userInterest.UserId != viewerUserId)
            .GroupBy(userInterest => userInterest.UserId)
            .Select(group => new { UserId = group.Key, SharedCount = group.Count() })
            .Join(
                dbContext.Users.AsNoTracking(),
                match => match.UserId,
                user => user.Id,
                (match, user) => new
                {
                    match.UserId,
                    match.SharedCount,
                    DisplayName = user.Profile != null ? user.Profile.DisplayName : user.DisplayName,
                    Headline = user.Profile != null ? user.Profile.Headline : null,
                    City = user.Profile != null ? user.Profile.City : null,
                    AvatarUrl = user.Profile != null ? user.Profile.AvatarUrl : null,
                    Skills = user.Profile != null ? user.Profile.Skills : new List<string>()
                })
            .OrderByDescending(row => row.SharedCount)
            .ThenBy(row => row.DisplayName)
            .ThenBy(row => row.UserId)
            .Take(take)
            .ToListAsync(cancellationToken);

        var matchedUserIds = ranked.Select(row => row.UserId).ToList();

        var sharedRows = await dbContext.UserInterests
            .AsNoTracking()
            .Where(userInterest =>
                matchedUserIds.Contains(userInterest.UserId) &&
                searchIds.Contains(userInterest.InterestId))
            .Select(userInterest => new
            {
                userInterest.UserId,
                userInterest.Interest.Id,
                userInterest.Interest.Name,
                userInterest.Interest.Color,
                userInterest.Interest.SortOrder
            })
            .ToListAsync(cancellationToken);

        var sharedByUser = sharedRows
            .GroupBy(row => row.UserId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<SharedInterestResponse>)group
                    .OrderBy(row => row.SortOrder)
                    .ThenBy(row => row.Name)
                    .Select(row => new SharedInterestResponse
                    {
                        Id = row.Id,
                        Name = row.Name,
                        Color = row.Color
                    })
                    .ToList());

        return ApplicationResult<PeopleSearchResponse>.Success(new PeopleSearchResponse
        {
            InterestIds = searchIds,
            UsedMyInterests = usedMyInterests,
            Items = ranked
                .Select(row => new PersonMatchResponse
                {
                    UserId = row.UserId,
                    DisplayName = row.DisplayName,
                    Headline = row.Headline,
                    City = row.City,
                    AvatarUrl = row.AvatarUrl,
                    SharedInterestCount = row.SharedCount,
                    SharedInterests = sharedByUser.GetValueOrDefault(row.UserId, []),
                    Skills = row.Skills
                })
                .ToList()
        });
    }
}

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
    internal const int MaxSkillFilters = 10;
    internal const int MaxSkillLength = 40;
    internal const int MaxAvailableSkills = 30;
    private const int AvailableSkillsCandidateSample = 200;

    public async Task<ApplicationResult<PeopleSearchResponse>> SearchByInterestsAsync(
        Guid viewerUserId,
        IReadOnlyCollection<Guid>? interestIds,
        int? limit,
        CancellationToken cancellationToken = default) =>
        await SearchAsync(viewerUserId, interestIds, skills: null, limit, cancellationToken);

    public async Task<ApplicationResult<PeopleSearchResponse>> SearchAsync(
        Guid viewerUserId,
        IReadOnlyCollection<Guid>? interestIds,
        IReadOnlyCollection<string>? skills,
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

        var (skillFilters, skillsError) = NormalizeSkillFilters(skills);

        if (skillsError is not null)
        {
            return ApplicationResult<PeopleSearchResponse>.Failure(
                ApplicationErrorKind.Validation,
                skillsError);
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
                SkillFilters = skillFilters,
                Items = []
            });
        }

        var candidates = dbContext.UserInterests
            .AsNoTracking()
            .Where(userInterest =>
                searchIds.Contains(userInterest.InterestId) &&
                userInterest.UserId != viewerUserId)
            .GroupBy(userInterest => userInterest.UserId)
            .Select(group => new { UserId = group.Key, SharedCount = group.Count() });

        // Skills offered as filter chips: taken from people who match the interests (before skill filtering).
        var candidateSkillLists = await candidates
            .OrderByDescending(candidate => candidate.SharedCount)
            .Take(AvailableSkillsCandidateSample)
            .Join(
                dbContext.UserProfiles.AsNoTracking(),
                candidate => candidate.UserId,
                profile => profile.UserId,
                (candidate, profile) => profile.Skills)
            .ToListAsync(cancellationToken);

        var availableSkills = candidateSkillLists
            .SelectMany(list => list)
            .GroupBy(skill => skill.Trim(), StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Take(MaxAvailableSkills)
            .Select(group => group.Key)
            .ToList();

        var users = dbContext.Users.AsNoTracking();

        foreach (var skill in skillFilters)
        {
            // AND semantics: every selected skill must be present (case-insensitive, evaluated in PostgreSQL).
            users = users.Where(user =>
                user.Profile != null &&
                user.Profile.Skills.Any(userSkill => userSkill.ToLower() == skill));
        }

        var ranked = await candidates
            .Join(
                users,
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
            SkillFilters = skillFilters,
            AvailableSkills = availableSkills,
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

    internal static (List<string> Skills, string? Error) NormalizeSkillFilters(IReadOnlyCollection<string>? skills)
    {
        if (skills is null || skills.Count == 0)
        {
            return ([], null);
        }

        var normalized = new List<string>();

        foreach (var raw in skills)
        {
            var skill = raw?.Trim();

            if (string.IsNullOrWhiteSpace(skill))
            {
                return ([], "Skill filters cannot be empty.");
            }

            if (skill.Length > MaxSkillLength)
            {
                return ([], $"Each skill filter must be at most {MaxSkillLength} characters.");
            }

            var lowered = skill.ToLowerInvariant();

            if (!normalized.Contains(lowered))
            {
                normalized.Add(lowered);
            }
        }

        return normalized.Count > MaxSkillFilters
            ? ([], $"At most {MaxSkillFilters} skills can be used as filters.")
            : (normalized, null);
    }
}

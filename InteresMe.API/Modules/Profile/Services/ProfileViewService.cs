using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.Data;
using InteresMe.API.Modules.Initiatives.Domain.Enums;
using InteresMe.API.Modules.Profile.DTOs.ProfileView;
using Microsoft.EntityFrameworkCore;

namespace InteresMe.API.Modules.Profile.Services;

public sealed class ProfileViewService(AppDbContext dbContext) : IProfileViewService
{
    private const int InitiativePreviewLimit = 6;
    private const int RecentHistoryLimit = 10;

    public async Task<ApplicationResult<ProfileViewResponse>> GetAsync(
        Guid viewerUserId,
        Guid profileUserId,
        CancellationToken cancellationToken = default)
    {
        var basicProfile = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == profileUserId)
            .Select(user => new BasicProfileResponse
            {
                UserId = user.Id,
                DisplayName = user.Profile != null ? user.Profile.DisplayName : user.DisplayName,
                AvatarUrl = user.Profile != null ? user.Profile.AvatarUrl : null,
                City = user.Profile != null ? user.Profile.City : null,
                JoinedAt = user.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (basicProfile is null)
        {
            return ApplicationResult<ProfileViewResponse>.Failure(
                ApplicationErrorKind.NotFound,
                ProfileErrorCodes.UserNotFound,
                "User was not found.");
        }

        var interestRows = await dbContext.UserInterests
            .AsNoTracking()
            .Where(userInterest => userInterest.UserId == profileUserId)
            .OrderBy(userInterest => userInterest.Interest.Category.SortOrder)
            .ThenBy(userInterest => userInterest.Interest.SortOrder)
            .Select(userInterest => new
            {
                CategoryId = userInterest.Interest.Category.Id,
                CategoryName = userInterest.Interest.Category.Name,
                CategorySlug = userInterest.Interest.Category.Slug,
                InterestId = userInterest.Interest.Id,
                InterestName = userInterest.Interest.Name,
                InterestSlug = userInterest.Interest.Slug
            })
            .ToListAsync(cancellationToken);

        var interests = interestRows
            .GroupBy(row => new { row.CategoryId, row.CategoryName, row.CategorySlug })
            .Select(group => new ProfileInterestCategoryResponse
            {
                Id = group.Key.CategoryId,
                Name = group.Key.CategoryName,
                Slug = group.Key.CategorySlug,
                Interests = group.Select(row => new ProfileInterestResponse
                {
                    Id = row.InterestId,
                    Name = row.InterestName,
                    Slug = row.InterestSlug
                }).ToList()
            })
            .ToList();

        var sharedInterestsCount = viewerUserId == profileUserId
            ? interestRows.Count
            : await dbContext.UserInterests
                .AsNoTracking()
                .Where(profileInterest =>
                    profileInterest.UserId == profileUserId &&
                    dbContext.UserInterests.Any(viewerInterest =>
                        viewerInterest.UserId == viewerUserId &&
                        viewerInterest.InterestId == profileInterest.InterestId))
                .CountAsync(cancellationToken);

        var createdInitiativesCount = await dbContext.Initiatives
            .AsNoTracking()
            .CountAsync(initiative => initiative.OwnerUserId == profileUserId, cancellationToken);

        var joinedInitiativesCount = await dbContext.InitiativeJoinRequests
            .AsNoTracking()
            .Where(joinRequest =>
                joinRequest.UserId == profileUserId &&
                joinRequest.Status == InitiativeJoinRequestStatus.Accepted)
            .Select(joinRequest => joinRequest.InitiativeId)
            .Distinct()
            .CountAsync(cancellationToken);

        var followersCount = await dbContext.UserFollows
            .AsNoTracking()
            .CountAsync(follow => follow.FollowedId == profileUserId, cancellationToken);

        var followingCount = await dbContext.UserFollows
            .AsNoTracking()
            .CountAsync(follow => follow.FollowerId == profileUserId, cancellationToken);

        var historyEventsCount = await dbContext.UserHistoryEvents
            .AsNoTracking()
            .CountAsync(historyEvent => historyEvent.UserId == profileUserId, cancellationToken);

        var isFollowing = viewerUserId != profileUserId && await dbContext.UserFollows
            .AsNoTracking()
            .AnyAsync(
                follow => follow.FollowerId == viewerUserId && follow.FollowedId == profileUserId,
                cancellationToken);

        var createdInitiatives = await dbContext.Initiatives
            .AsNoTracking()
            .Where(initiative => initiative.OwnerUserId == profileUserId)
            .OrderByDescending(initiative => initiative.CreatedAt)
            .Take(InitiativePreviewLimit)
            .Select(initiative => new ProfileInitiativePreviewResponse
            {
                Id = initiative.Id,
                Slug = initiative.Slug,
                Title = initiative.Title,
                ShortDescription = initiative.ShortDescription,
                GoalType = initiative.GoalType,
                Status = initiative.Status,
                Visibility = initiative.Visibility,
                CreatedAt = initiative.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var joinedInitiatives = await dbContext.InitiativeJoinRequests
            .AsNoTracking()
            .Where(joinRequest =>
                joinRequest.UserId == profileUserId &&
                joinRequest.Status == InitiativeJoinRequestStatus.Accepted)
            .GroupBy(joinRequest => new
            {
                joinRequest.Initiative.Id,
                joinRequest.Initiative.Slug,
                joinRequest.Initiative.Title,
                joinRequest.Initiative.ShortDescription,
                joinRequest.Initiative.GoalType,
                joinRequest.Initiative.Status,
                joinRequest.Initiative.Visibility,
                joinRequest.Initiative.CreatedAt
            })
            .OrderByDescending(group => group.Max(joinRequest => joinRequest.UpdatedAt))
            .Take(InitiativePreviewLimit)
            .Select(group => new ProfileInitiativePreviewResponse
            {
                Id = group.Key.Id,
                Slug = group.Key.Slug,
                Title = group.Key.Title,
                ShortDescription = group.Key.ShortDescription,
                GoalType = group.Key.GoalType,
                Status = group.Key.Status,
                Visibility = group.Key.Visibility,
                CreatedAt = group.Key.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var recentHistory = await dbContext.UserHistoryEvents
            .AsNoTracking()
            .Where(historyEvent => historyEvent.UserId == profileUserId)
            .OrderByDescending(historyEvent => historyEvent.OccurredAt)
            .ThenByDescending(historyEvent => historyEvent.CreatedAt)
            .Take(RecentHistoryLimit)
            .Select(historyEvent => new ProfileHistoryPreviewResponse
            {
                Id = historyEvent.Id,
                Type = historyEvent.Type,
                Title = historyEvent.Title,
                Description = historyEvent.Description,
                TargetType = historyEvent.TargetType,
                TargetId = historyEvent.TargetId,
                TargetName = historyEvent.TargetName,
                MetadataJson = historyEvent.MetadataJson,
                OccurredAt = historyEvent.OccurredAt
            })
            .ToListAsync(cancellationToken);

        var response = new ProfileViewResponse
        {
            BasicProfile = basicProfile,
            Stats = new ProfileStatsResponse
            {
                InterestsCount = interestRows.Count,
                SharedInterestsCount = sharedInterestsCount,
                CreatedInitiativesCount = createdInitiativesCount,
                JoinedInitiativesCount = joinedInitiativesCount,
                FollowersCount = followersCount,
                FollowingCount = followingCount,
                HistoryEventsCount = historyEventsCount,
                PostsCount = 0
            },
            ViewerRelation = new ViewerRelationResponse
            {
                IsFollowing = isFollowing,
                CanFollow = viewerUserId != profileUserId && !isFollowing,
                CanMessage = viewerUserId != profileUserId
            },
            Interests = interests,
            CreatedInitiatives = createdInitiatives,
            JoinedInitiatives = joinedInitiatives,
            RecentHistory = recentHistory,
            RecentPosts = []
        };

        return ApplicationResult<ProfileViewResponse>.Success(response);
    }

    public async Task<ApplicationResult<bool>> FollowAsync(
        Guid followerId,
        Guid followedId,
        CancellationToken cancellationToken = default)
    {
        var validation = await ValidateFollowUsersAsync(followerId, followedId, cancellationToken);
        if (validation is not null)
        {
            return validation;
        }

        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO profile.user_follows ("FollowerId", "FollowedId", "CreatedAt")
            VALUES ({followerId}, {followedId}, {DateTime.UtcNow})
            ON CONFLICT ("FollowerId", "FollowedId") DO NOTHING
            """,
            cancellationToken);

        return ApplicationResult<bool>.Success(true);
    }

    public async Task<ApplicationResult<bool>> UnfollowAsync(
        Guid followerId,
        Guid followedId,
        CancellationToken cancellationToken = default)
    {
        var validation = await ValidateFollowUsersAsync(followerId, followedId, cancellationToken);
        if (validation is not null)
        {
            return validation;
        }

        await dbContext.UserFollows
            .Where(follow => follow.FollowerId == followerId && follow.FollowedId == followedId)
            .ExecuteDeleteAsync(cancellationToken);

        return ApplicationResult<bool>.Success(true);
    }

    private async Task<ApplicationResult<bool>?> ValidateFollowUsersAsync(
        Guid followerId,
        Guid followedId,
        CancellationToken cancellationToken)
    {
        if (followerId == followedId)
        {
            return ApplicationResult<bool>.Failure(
                ApplicationErrorKind.Validation,
                ProfileErrorCodes.FollowSelfNotAllowed,
                "You cannot follow yourself.");
        }

        var followedUserExists = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(user => user.Id == followedId, cancellationToken);

        return followedUserExists
            ? null
            : ApplicationResult<bool>.Failure(
                ApplicationErrorKind.NotFound,
                ProfileErrorCodes.UserNotFound,
                "User was not found.");
    }

}

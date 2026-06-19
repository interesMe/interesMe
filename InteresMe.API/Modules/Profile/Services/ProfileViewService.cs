using System.Text.Json;
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
    private const int RecentPostsLimit = 6;

    public async Task<ApplicationResult<ProfileViewResponse>> GetAsync(
        Guid viewerUserId,
        Guid profileUserId,
        CancellationToken cancellationToken = default)
    {
        var profileData = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == profileUserId)
            .Select(user => new
            {
                UserId = user.Id,
                DisplayName = user.Profile != null ? user.Profile.DisplayName : user.DisplayName,
                AvatarUrl = user.Profile != null ? user.Profile.AvatarUrl : null,
                City = user.Profile != null ? user.Profile.City : null,
                Bio = user.Profile != null ? user.Profile.Bio : null,
                SocialLinksJson = user.Profile != null ? user.Profile.SocialLinksJson : null,
                ProfileStatus = user.Profile != null ? user.Profile.ProfileStatus : null,
                IsDemoUser = user.Email.EndsWith("@demo.interesme.local"),
                JoinedAt = user.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (profileData is null)
        {
            return ApplicationResult<ProfileViewResponse>.Failure(
                ApplicationErrorKind.NotFound,
                ProfileErrorCodes.UserNotFound,
                "User was not found.");
        }

        var basicProfile = new BasicProfileResponse
        {
            UserId = profileData.UserId,
            DisplayName = profileData.DisplayName,
            AvatarUrl = profileData.AvatarUrl,
            City = profileData.City,
            Bio = profileData.Bio,
            ActivityStatus = GetActivityStatus(
                viewerUserId,
                profileUserId,
                profileData.IsDemoUser,
                profileData.DisplayName),
            ProfileStatus = profileData.ProfileStatus ?? "active_member",
            SocialLinks = ParseSocialLinks(profileData.SocialLinksJson),
            JoinedAt = profileData.JoinedAt
        };

        var parentInterestRows = await dbContext.UserInterests
            .AsNoTracking()
            .Where(userInterest => userInterest.UserId == profileUserId)
            .Select(userInterest => new
            {
                CategoryId = userInterest.Interest.Category.Id,
                CategoryName = userInterest.Interest.Category.Name,
                CategorySlug = userInterest.Interest.Category.Slug,
                CategorySortOrder = userInterest.Interest.Category.SortOrder,
                ParentInterestId = userInterest.Interest.Id,
                InterestId = userInterest.Interest.Id,
                InterestName = userInterest.Interest.Name,
                InterestSlug = userInterest.Interest.Slug,
                InterestSortOrder = userInterest.Interest.SortOrder
            })
            .ToListAsync(cancellationToken);

        var subinterestRows = await dbContext.UserSubinterests
            .AsNoTracking()
            .Where(selection => selection.UserId == profileUserId)
            .Select(selection => new
            {
                CategoryId = selection.Subinterest.Interest.Category.Id,
                CategoryName = selection.Subinterest.Interest.Category.Name,
                CategorySlug = selection.Subinterest.Interest.Category.Slug,
                CategorySortOrder = selection.Subinterest.Interest.Category.SortOrder,
                ParentInterestId = selection.Subinterest.InterestId,
                InterestId = selection.Subinterest.Id,
                InterestName = selection.Subinterest.Name,
                InterestSlug = selection.Subinterest.Slug,
                InterestSortOrder = selection.Subinterest.SortOrder
            })
            .ToListAsync(cancellationToken);

        var selectedParentIds = subinterestRows
            .Select(row => row.ParentInterestId)
            .ToHashSet();
        var interestRows = parentInterestRows
            .Where(row => !selectedParentIds.Contains(row.ParentInterestId))
            .Select(row => new ProfileInterestRow(
                row.CategoryId,
                row.CategoryName,
                row.CategorySlug,
                row.CategorySortOrder,
                row.ParentInterestId,
                row.InterestId,
                row.InterestName,
                row.InterestSlug,
                row.InterestSortOrder))
            .Concat(subinterestRows.Select(row => new ProfileInterestRow(
                row.CategoryId,
                row.CategoryName,
                row.CategorySlug,
                row.CategorySortOrder,
                row.ParentInterestId,
                row.InterestId,
                row.InterestName,
                row.InterestSlug,
                row.InterestSortOrder)))
            .OrderBy(row => row.CategorySortOrder)
            .ThenBy(row => row.InterestSortOrder)
            .ToList();

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

        var sharedInterestsCount = interestRows.Count;
        if (viewerUserId != profileUserId)
        {
            var viewerParentInterestIds = await dbContext.UserInterests
                .AsNoTracking()
                .Where(viewerInterest => viewerInterest.UserId == viewerUserId)
                .Select(viewerInterest => viewerInterest.InterestId)
                .ToListAsync(cancellationToken);

            sharedInterestsCount = interestRows.Count(row =>
                viewerParentInterestIds.Contains(row.ParentInterestId));
        }

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

        var postsCount = await dbContext.ProfilePosts
            .AsNoTracking()
            .CountAsync(post => post.UserId == profileUserId, cancellationToken);

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

        var recentPostRows = await dbContext.ProfilePosts
            .AsNoTracking()
            .Where(post => post.UserId == profileUserId)
            .OrderByDescending(post => post.CreatedAt)
            .Take(RecentPostsLimit)
            .Select(post => new
            {
                post.Id,
                post.Title,
                post.Body,
                post.Type,
                post.InterestPath,
                post.MediaUrlsJson,
                post.CreatedAt
            })
            .ToListAsync(cancellationToken);
        var recentPosts = recentPostRows
            .Select(post => new ProfilePostPreviewResponse
            {
                Id = post.Id,
                Title = post.Title,
                Body = post.Body,
                Type = post.Type,
                InterestPath = post.InterestPath,
                ImageUrls = ParseMediaUrls(post.MediaUrlsJson),
                CreatedAt = post.CreatedAt
            })
            .ToList();

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
                PostsCount = postsCount
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
            RecentPosts = recentPosts
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

    private static string GetActivityStatus(
        Guid viewerUserId,
        Guid profileUserId,
        bool isDemoUser,
        string displayName)
    {
        if (viewerUserId == profileUserId)
        {
            return "online";
        }

        if (!isDemoUser)
        {
            return "unknown";
        }

        return displayName is "Anna Designer" or "Alex Developer" or "Olivia AI Researcher"
            ? "recently_active"
            : "offline";
    }

    private static List<ProfileSocialLinkResponse> ParseSocialLinks(string? socialLinksJson)
    {
        if (string.IsNullOrWhiteSpace(socialLinksJson))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<ProfileSocialLinkResponse>>(
                socialLinksJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static List<string> ParseMediaUrls(string? mediaUrlsJson)
    {
        if (string.IsNullOrWhiteSpace(mediaUrlsJson))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(mediaUrlsJson) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private sealed record ProfileInterestRow(
        Guid CategoryId,
        string CategoryName,
        string CategorySlug,
        int CategorySortOrder,
        Guid ParentInterestId,
        Guid InterestId,
        string InterestName,
        string InterestSlug,
        int InterestSortOrder);

}

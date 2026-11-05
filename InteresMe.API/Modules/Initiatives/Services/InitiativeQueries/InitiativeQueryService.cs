using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.Data;
using InteresMe.API.Modules.Initiatives;
using InteresMe.API.Modules.Initiatives.Contracts.Responses;
using InteresMe.API.Modules.Initiatives.Domain.Entities;
using InteresMe.API.Modules.Initiatives.Domain.Enums;
using InteresMe.API.Modules.Initiatives.Mapping;
using Microsoft.EntityFrameworkCore;

namespace InteresMe.API.Modules.Initiatives.Services.InitiativeQueries;

public sealed class InitiativeQueryService(AppDbContext dbContext) : IInitiativeQueryService
{
    internal const int DefaultLimit = 50;
    internal const int MaxLimit = 100;
    internal const int MaxSearchLength = 100;

    public async Task<ApplicationResult<List<InitiativeResponse>>> GetAllAsync(
        Guid viewerUserId,
        string? search = null,
        InitiativeGoalType? goalType = null,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var take = limit ?? DefaultLimit;

        if (take < 1 || take > MaxLimit)
        {
            return ApplicationResult<List<InitiativeResponse>>.Failure(
                ApplicationErrorKind.Validation,
                InitiativeErrorCodes.QueryInvalid,
                $"Limit must be between 1 and {MaxLimit}.");
        }

        if (goalType is not null && !Enum.IsDefined(goalType.Value))
        {
            return ApplicationResult<List<InitiativeResponse>>.Failure(
                ApplicationErrorKind.Validation,
                InitiativeErrorCodes.QueryInvalid,
                "Goal type is invalid.");
        }

        var normalizedSearch = search?.Trim();

        if (normalizedSearch?.Length > MaxSearchLength)
        {
            return ApplicationResult<List<InitiativeResponse>>.Failure(
                ApplicationErrorKind.Validation,
                InitiativeErrorCodes.QueryInvalid,
                $"Search must be at most {MaxSearchLength} characters.");
        }

        var query = BaseInitiativeQuery()
            .Where(initiative =>
                (initiative.Visibility == InitiativeVisibility.Public || initiative.OwnerUserId == viewerUserId) &&
                initiative.Status != InitiativeStatus.Completed &&
                initiative.Status != InitiativeStatus.Archived);

        if (goalType is not null)
        {
            query = query.Where(initiative => initiative.GoalType == goalType.Value);
        }

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            var pattern = $"%{EscapeLikePattern(normalizedSearch)}%";
            query = query.Where(initiative =>
                EF.Functions.ILike(initiative.Title, pattern, "\\") ||
                EF.Functions.ILike(initiative.ShortDescription, pattern, "\\"));
        }

        var initiatives = await query
            .OrderByDescending(initiative => initiative.UpdatedAt)
            .ThenBy(initiative => initiative.Id)
            .Take(take)
            .ToListAsync(cancellationToken);

        var responses = initiatives.Select(InitiativeMapper.ToResponse).ToList();
        await FillMemberCountsAsync(responses, cancellationToken);

        return ApplicationResult<List<InitiativeResponse>>.Success(responses);
    }

    public async Task<List<InitiativeResponse>> GetMineAsync(
        Guid viewerUserId,
        CancellationToken cancellationToken = default)
    {
        var initiatives = await BaseInitiativeQuery()
            .Where(initiative => initiative.OwnerUserId == viewerUserId)
            .OrderByDescending(initiative => initiative.UpdatedAt)
            .ToListAsync(cancellationToken);

        var responses = initiatives.Select(InitiativeMapper.ToResponse).ToList();
        await FillMemberCountsAsync(responses, cancellationToken);

        return responses;
    }

    public async Task<ApplicationResult<InitiativeResponse>> GetByIdAsync(
        Guid viewerUserId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var initiative = await BaseInitiativeQuery()
            .FirstOrDefaultAsync(
                currentInitiative => currentInitiative.Id == id,
                cancellationToken);

        if (initiative is null)
        {
            return NotFound();
        }

        var viewerRequest = await dbContext.InitiativeJoinRequests
            .AsNoTracking()
            .Where(joinRequest => joinRequest.InitiativeId == id && joinRequest.UserId == viewerUserId)
            .OrderByDescending(joinRequest => joinRequest.CreatedAt)
            .Select(joinRequest => new ViewerJoinRequestResponse
            {
                Id = joinRequest.Id,
                Status = joinRequest.Status,
                RoleId = joinRequest.RoleId,
                CreatedAt = joinRequest.CreatedAt,
                UpdatedAt = joinRequest.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        var isOwner = initiative.OwnerUserId == viewerUserId;
        var isMember = viewerRequest?.Status == InitiativeJoinRequestStatus.Accepted;

        if (initiative.Visibility != InitiativeVisibility.Public && !isOwner && !isMember)
        {
            return NotFound();
        }

        var response = InitiativeMapper.ToResponse(initiative);
        response.MyJoinRequest = viewerRequest;
        response.Members = await dbContext.InitiativeJoinRequests
            .AsNoTracking()
            .Where(joinRequest =>
                joinRequest.InitiativeId == id &&
                joinRequest.Status == InitiativeJoinRequestStatus.Accepted)
            .OrderBy(joinRequest => joinRequest.UpdatedAt)
            .Select(joinRequest => new InitiativeMemberResponse
            {
                UserId = joinRequest.UserId,
                DisplayName = joinRequest.User.Profile != null
                    ? joinRequest.User.Profile.DisplayName
                    : joinRequest.User.DisplayName,
                AvatarUrl = joinRequest.User.Profile != null ? joinRequest.User.Profile.AvatarUrl : null,
                RoleName = joinRequest.Role != null ? joinRequest.Role.Name : null,
                JoinedAt = joinRequest.UpdatedAt
            })
            .ToListAsync(cancellationToken);
        response.MemberCount = response.Members.Count;

        return ApplicationResult<InitiativeResponse>.Success(response);
    }

    public async Task<ApplicationResult<PublicInitiativeResponse>> GetPublicBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        var normalizedSlug = slug.Trim();

        var initiative = await dbContext.Initiatives
            .AsNoTracking()
            .AsSplitQuery()
            .Where(currentInitiative =>
                currentInitiative.Slug == normalizedSlug &&
                currentInitiative.Visibility == InitiativeVisibility.Public &&
                currentInitiative.Status != InitiativeStatus.Archived)
            .Select(InitiativeMapper.PublicResponseProjection)
            .FirstOrDefaultAsync(cancellationToken);

        if (initiative is null)
        {
            return ApplicationResult<PublicInitiativeResponse>.Failure(
                ApplicationErrorKind.NotFound,
                InitiativeErrorCodes.NotFound,
                "Initiative was not found.");
        }

        return ApplicationResult<PublicInitiativeResponse>.Success(initiative);
    }

    private async Task FillMemberCountsAsync(
        List<InitiativeResponse> responses,
        CancellationToken cancellationToken)
    {
        if (responses.Count == 0)
        {
            return;
        }

        var ids = responses.Select(response => response.Id).ToList();
        var counts = await dbContext.InitiativeJoinRequests
            .AsNoTracking()
            .Where(joinRequest =>
                ids.Contains(joinRequest.InitiativeId) &&
                joinRequest.Status == InitiativeJoinRequestStatus.Accepted)
            .GroupBy(joinRequest => joinRequest.InitiativeId)
            .Select(group => new { InitiativeId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(row => row.InitiativeId, row => row.Count, cancellationToken);

        foreach (var response in responses)
        {
            response.MemberCount = counts.GetValueOrDefault(response.Id);
        }
    }

    private static ApplicationResult<InitiativeResponse> NotFound() =>
        ApplicationResult<InitiativeResponse>.Failure(
            ApplicationErrorKind.NotFound,
            InitiativeErrorCodes.NotFound,
            "Initiative was not found.");

    private static string EscapeLikePattern(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    private IQueryable<Initiative> BaseInitiativeQuery() =>
        dbContext.Initiatives
            .AsNoTracking()
            .AsSplitQuery()
            .Include(initiative => initiative.OwnerUser)
                .ThenInclude(owner => owner.Profile)
            .Include(initiative => initiative.InitiativeInterests)
                .ThenInclude(initiativeInterest => initiativeInterest.Interest)
            .Include(initiative => initiative.Roles);
}

using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.Data;
using InteresMe.API.Modules.Initiatives.DTOs;
using InteresMe.API.Modules.Initiatives.Models;
using Microsoft.EntityFrameworkCore;

namespace InteresMe.API.Modules.Initiatives.Services;

public sealed class InitiativeService(AppDbContext dbContext) : IInitiativeService
{
    private const int TitleMaxLength = 120;
    private const int ShortDescriptionMaxLength = 500;
    private const int UniversityMaxLength = 160;
    private const int RoleNameMaxLength = 80;
    private const int JoinRequestMessageMaxLength = 500;

    public async Task<List<InitiativeResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var initiatives = await BaseInitiativeQuery()
            .Where(initiative =>
                initiative.Status != InitiativeStatus.Completed &&
                initiative.Status != InitiativeStatus.Archived)
            .OrderByDescending(initiative => initiative.UpdatedAt)
            .ToListAsync(cancellationToken);

        return initiatives
            .Select(ToResponse)
            .ToList();
    }

    public async Task<ApplicationResult<InitiativeResponse>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var initiative = await BaseInitiativeQuery()
            .FirstOrDefaultAsync(
                currentInitiative => currentInitiative.Id == id,
                cancellationToken);

        if (initiative is null)
        {
            return ApplicationResult<InitiativeResponse>.Failure(
                ApplicationErrorKind.NotFound,
                "Initiative was not found.");
        }

        return ApplicationResult<InitiativeResponse>.Success(
            ToResponse(initiative));
    }

    public async Task<ApplicationResult<InitiativeResponse>> CreateAsync(
        Guid ownerUserId,
        CreateInitiativeRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeRequest(request);
        var validation = await ValidateInitiativeAsync(
            normalized,
            cancellationToken);

        if (validation is not null)
        {
            return ApplicationResult<InitiativeResponse>.Failure(
                ApplicationErrorKind.Validation,
                validation);
        }

        var ownerExists = await dbContext.Users
            .AnyAsync(user => user.Id == ownerUserId, cancellationToken);

        if (!ownerExists)
        {
            return ApplicationResult<InitiativeResponse>.Failure(
                ApplicationErrorKind.NotFound,
                "User was not found.");
        }

        var now = DateTime.UtcNow;
        var initiative = new Initiative
        {
            Id = Guid.NewGuid(),
            OwnerUserId = ownerUserId,
            Title = normalized.Title,
            ShortDescription = normalized.ShortDescription,
            GoalType = normalized.GoalType,
            University = normalized.University,
            TeamSize = normalized.TeamSize,
            Status = normalized.Status ?? InitiativeStatus.Idea,
            CreatedAt = now,
            UpdatedAt = now
        };

        ApplyInterests(initiative, normalized.InterestIds);
        ApplyRoles(initiative, normalized.Roles);

        dbContext.Initiatives.Add(initiative);
        await dbContext.SaveChangesAsync(cancellationToken);

        var created = await BaseInitiativeQuery()
            .FirstAsync(
                currentInitiative => currentInitiative.Id == initiative.Id,
                cancellationToken);

        return ApplicationResult<InitiativeResponse>.Success(ToResponse(created));
    }

    public async Task<ApplicationResult<InitiativeResponse>> UpdateAsync(
        Guid ownerUserId,
        Guid id,
        UpdateInitiativeRequest request,
        CancellationToken cancellationToken = default)
    {
        var initiative = await dbContext.Initiatives
            .Include(currentInitiative => currentInitiative.InitiativeInterests)
            .Include(currentInitiative => currentInitiative.Roles)
            .FirstOrDefaultAsync(
                currentInitiative => currentInitiative.Id == id,
                cancellationToken);

        if (initiative is null)
        {
            return ApplicationResult<InitiativeResponse>.Failure(
                ApplicationErrorKind.NotFound,
                "Initiative was not found.");
        }

        if (initiative.OwnerUserId != ownerUserId)
        {
            return ApplicationResult<InitiativeResponse>.Failure(
                ApplicationErrorKind.Forbidden,
                "Only the initiative owner can update this initiative.");
        }

        var normalized = NormalizeRequest(request);
        var validation = await ValidateInitiativeAsync(
            normalized,
            cancellationToken);

        if (validation is not null)
        {
            return ApplicationResult<InitiativeResponse>.Failure(
                ApplicationErrorKind.Validation,
                validation);
        }

        initiative.Title = normalized.Title;
        initiative.ShortDescription = normalized.ShortDescription;
        initiative.GoalType = normalized.GoalType;
        initiative.University = normalized.University;
        initiative.TeamSize = normalized.TeamSize;
        initiative.Status = normalized.Status ?? initiative.Status;
        initiative.UpdatedAt = DateTime.UtcNow;

        dbContext.InitiativeInterests.RemoveRange(initiative.InitiativeInterests);
        initiative.InitiativeInterests.Clear();
        ApplyInterests(initiative, normalized.InterestIds);

        dbContext.InitiativeRoles.RemoveRange(initiative.Roles);
        initiative.Roles.Clear();
        ApplyRoles(initiative, normalized.Roles);

        await dbContext.SaveChangesAsync(cancellationToken);

        var updated = await BaseInitiativeQuery()
            .FirstAsync(
                currentInitiative => currentInitiative.Id == initiative.Id,
                cancellationToken);

        return ApplicationResult<InitiativeResponse>.Success(ToResponse(updated));
    }

    public async Task<ApplicationResult<bool>> DeleteAsync(
        Guid ownerUserId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var initiative = await dbContext.Initiatives
            .FirstOrDefaultAsync(
                currentInitiative => currentInitiative.Id == id,
                cancellationToken);

        if (initiative is null)
        {
            return ApplicationResult<bool>.Failure(
                ApplicationErrorKind.NotFound,
                "Initiative was not found.");
        }

        if (initiative.OwnerUserId != ownerUserId)
        {
            return ApplicationResult<bool>.Failure(
                ApplicationErrorKind.Forbidden,
                "Only the initiative owner can delete this initiative.");
        }

        var hasAcceptedMembers = await dbContext.InitiativeJoinRequests
            .AnyAsync(
                joinRequest =>
                    joinRequest.InitiativeId == id &&
                    joinRequest.Status == InitiativeJoinRequestStatus.Accepted,
                cancellationToken);

        if (hasAcceptedMembers)
        {
            initiative.Status = InitiativeStatus.Archived;
            initiative.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            dbContext.Initiatives.Remove(initiative);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return ApplicationResult<bool>.Success(true);
    }

    public async Task<ApplicationResult<InitiativeJoinRequestResponse>> CreateJoinRequestAsync(
        Guid userId,
        Guid initiativeId,
        CreateInitiativeJoinRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        var initiative = await dbContext.Initiatives
            .AsNoTracking()
            .FirstOrDefaultAsync(
                currentInitiative => currentInitiative.Id == initiativeId,
                cancellationToken);

        if (initiative is null)
        {
            return ApplicationResult<InitiativeJoinRequestResponse>.Failure(
                ApplicationErrorKind.NotFound,
                "Initiative was not found.");
        }

        if (initiative.OwnerUserId == userId)
        {
            return ApplicationResult<InitiativeJoinRequestResponse>.Failure(
                ApplicationErrorKind.Conflict,
                "Initiative owner cannot request to join their own initiative.");
        }

        var message = NormalizeOptional(request.Message);

        if (message?.Length > JoinRequestMessageMaxLength)
        {
            return ApplicationResult<InitiativeJoinRequestResponse>.Failure(
                ApplicationErrorKind.Validation,
                $"Message must be at most {JoinRequestMessageMaxLength} characters.");
        }

        var duplicatePendingRequest = await dbContext.InitiativeJoinRequests
            .AnyAsync(
                joinRequest =>
                    joinRequest.InitiativeId == initiativeId &&
                    joinRequest.UserId == userId &&
                    joinRequest.Status == InitiativeJoinRequestStatus.Pending,
                cancellationToken);

        if (duplicatePendingRequest)
        {
            return ApplicationResult<InitiativeJoinRequestResponse>.Failure(
                ApplicationErrorKind.Conflict,
                "You already have a pending join request for this initiative.");
        }

        var now = DateTime.UtcNow;
        var created = new InitiativeJoinRequest
        {
            Id = Guid.NewGuid(),
            InitiativeId = initiativeId,
            UserId = userId,
            Message = message,
            Status = InitiativeJoinRequestStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.InitiativeJoinRequests.Add(created);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ApplicationResult<InitiativeJoinRequestResponse>.Success(
            ToJoinRequestResponse(created));
    }

    public async Task<ApplicationResult<List<InitiativeJoinRequestResponse>>> GetJoinRequestsAsync(
        Guid ownerUserId,
        Guid initiativeId,
        CancellationToken cancellationToken = default)
    {
        var accessError = await VerifyOwnerAccessAsync(
            ownerUserId,
            initiativeId,
            cancellationToken);

        if (accessError is not null)
        {
            return ApplicationResult<List<InitiativeJoinRequestResponse>>.Failure(
                accessError.Value.Kind,
                accessError.Value.Message);
        }

        var requests = await dbContext.InitiativeJoinRequests
            .AsNoTracking()
            .Where(joinRequest => joinRequest.InitiativeId == initiativeId)
            .OrderByDescending(joinRequest => joinRequest.CreatedAt)
            .ToListAsync(cancellationToken);

        return ApplicationResult<List<InitiativeJoinRequestResponse>>.Success(
            requests.Select(ToJoinRequestResponse).ToList());
    }

    public async Task<ApplicationResult<InitiativeJoinRequestResponse>> AcceptJoinRequestAsync(
        Guid ownerUserId,
        Guid initiativeId,
        Guid requestId,
        CancellationToken cancellationToken = default) =>
        await UpdateJoinRequestStatusAsync(
            ownerUserId,
            initiativeId,
            requestId,
            InitiativeJoinRequestStatus.Accepted,
            cancellationToken);

    public async Task<ApplicationResult<InitiativeJoinRequestResponse>> RejectJoinRequestAsync(
        Guid ownerUserId,
        Guid initiativeId,
        Guid requestId,
        CancellationToken cancellationToken = default) =>
        await UpdateJoinRequestStatusAsync(
            ownerUserId,
            initiativeId,
            requestId,
            InitiativeJoinRequestStatus.Rejected,
            cancellationToken);

    private async Task<ApplicationResult<InitiativeJoinRequestResponse>> UpdateJoinRequestStatusAsync(
        Guid ownerUserId,
        Guid initiativeId,
        Guid requestId,
        InitiativeJoinRequestStatus status,
        CancellationToken cancellationToken)
    {
        var accessError = await VerifyOwnerAccessAsync(
            ownerUserId,
            initiativeId,
            cancellationToken);

        if (accessError is not null)
        {
            return ApplicationResult<InitiativeJoinRequestResponse>.Failure(
                accessError.Value.Kind,
                accessError.Value.Message);
        }

        var joinRequest = await dbContext.InitiativeJoinRequests
            .FirstOrDefaultAsync(
                currentRequest =>
                    currentRequest.Id == requestId &&
                    currentRequest.InitiativeId == initiativeId,
                cancellationToken);

        if (joinRequest is null)
        {
            return ApplicationResult<InitiativeJoinRequestResponse>.Failure(
                ApplicationErrorKind.NotFound,
                "Join request was not found.");
        }

        if (joinRequest.Status != InitiativeJoinRequestStatus.Pending)
        {
            return ApplicationResult<InitiativeJoinRequestResponse>.Failure(
                ApplicationErrorKind.Conflict,
                "Only pending join requests can be updated.");
        }

        joinRequest.Status = status;
        joinRequest.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return ApplicationResult<InitiativeJoinRequestResponse>.Success(
            ToJoinRequestResponse(joinRequest));
    }

    private async Task<OwnerAccessError?> VerifyOwnerAccessAsync(
        Guid ownerUserId,
        Guid initiativeId,
        CancellationToken cancellationToken)
    {
        var initiative = await dbContext.Initiatives
            .AsNoTracking()
            .FirstOrDefaultAsync(
                currentInitiative => currentInitiative.Id == initiativeId,
                cancellationToken);

        if (initiative is null)
        {
            return new OwnerAccessError(
                ApplicationErrorKind.NotFound,
                "Initiative was not found.");
        }

        if (initiative.OwnerUserId != ownerUserId)
        {
            return new OwnerAccessError(
                ApplicationErrorKind.Forbidden,
                "Only the initiative owner can manage join requests.");
        }

        return null;
    }

    private IQueryable<Initiative> BaseInitiativeQuery() =>
        dbContext.Initiatives
            .AsNoTracking()
            .AsSplitQuery()
            .Include(initiative => initiative.InitiativeInterests)
                .ThenInclude(initiativeInterest => initiativeInterest.Interest)
            .Include(initiative => initiative.Roles);

    private async Task<string?> ValidateInitiativeAsync(
        NormalizedInitiativeRequest request,
        CancellationToken cancellationToken)
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

    private static NormalizedInitiativeRequest NormalizeRequest(
        CreateInitiativeRequest request) => new(
        request.Title?.Trim() ?? string.Empty,
        request.ShortDescription?.Trim() ?? string.Empty,
        request.GoalType,
        NormalizeInterestIds(request.InterestIds),
        NormalizeRoles(request.Roles),
        NormalizeOptional(request.University),
        request.TeamSize,
        request.Status);

    private static NormalizedInitiativeRequest NormalizeRequest(
        UpdateInitiativeRequest request) => new(
        request.Title?.Trim() ?? string.Empty,
        request.ShortDescription?.Trim() ?? string.Empty,
        request.GoalType,
        NormalizeInterestIds(request.InterestIds),
        NormalizeRoles(request.Roles),
        NormalizeOptional(request.University),
        request.TeamSize,
        request.Status);

    private static List<Guid> NormalizeInterestIds(List<Guid>? interestIds) =>
        interestIds?.Distinct().ToList() ?? [];

    private static List<string> NormalizeRoles(List<string>? roles) =>
        roles?
            .Select(role => role.Trim())
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();

        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static void ApplyInterests(
        Initiative initiative,
        List<Guid> interestIds)
    {
        foreach (var interestId in interestIds)
        {
            initiative.InitiativeInterests.Add(new InitiativeInterest
            {
                InitiativeId = initiative.Id,
                InterestId = interestId
            });
        }
    }

    private static void ApplyRoles(
        Initiative initiative,
        List<string> roles)
    {
        foreach (var role in roles)
        {
            initiative.Roles.Add(new InitiativeRole
            {
                Id = Guid.NewGuid(),
                InitiativeId = initiative.Id,
                Name = role
            });
        }
    }

    private static InitiativeResponse ToResponse(Initiative initiative) => new()
    {
        Id = initiative.Id,
        OwnerUserId = initiative.OwnerUserId,
        Title = initiative.Title,
        ShortDescription = initiative.ShortDescription,
        GoalType = initiative.GoalType,
        University = initiative.University,
        TeamSize = initiative.TeamSize,
        Status = initiative.Status,
        CreatedAt = initiative.CreatedAt,
        UpdatedAt = initiative.UpdatedAt,
        Interests = initiative.InitiativeInterests
            .OrderBy(initiativeInterest => initiativeInterest.Interest.Name)
            .Select(initiativeInterest => new InitiativeInterestResponse
            {
                Id = initiativeInterest.Interest.Id,
                Name = initiativeInterest.Interest.Name,
                Slug = initiativeInterest.Interest.Slug
            })
            .ToList(),
        Roles = initiative.Roles
            .OrderBy(role => role.Name)
            .Select(role => new InitiativeRoleResponse
            {
                Id = role.Id,
                Name = role.Name
            })
            .ToList()
    };

    private static InitiativeJoinRequestResponse ToJoinRequestResponse(
        InitiativeJoinRequest joinRequest) => new()
    {
        Id = joinRequest.Id,
        InitiativeId = joinRequest.InitiativeId,
        UserId = joinRequest.UserId,
        Message = joinRequest.Message,
        Status = joinRequest.Status,
        CreatedAt = joinRequest.CreatedAt,
        UpdatedAt = joinRequest.UpdatedAt
    };

    private sealed record NormalizedInitiativeRequest(
        string Title,
        string ShortDescription,
        InitiativeGoalType GoalType,
        List<Guid> InterestIds,
        List<string> Roles,
        string? University,
        int? TeamSize,
        InitiativeStatus? Status);

    private readonly record struct OwnerAccessError(
        ApplicationErrorKind Kind,
        string Message);
}

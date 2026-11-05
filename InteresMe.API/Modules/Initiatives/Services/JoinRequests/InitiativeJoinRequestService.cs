using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.Data;
using InteresMe.API.Modules.Initiatives;
using InteresMe.API.Modules.Initiatives.Contracts.Requests;
using InteresMe.API.Modules.Initiatives.Contracts.Responses;
using InteresMe.API.Modules.Initiatives.Domain.Entities;
using InteresMe.API.Modules.Initiatives.Domain.Enums;
using InteresMe.API.Modules.Initiatives.Mapping;
using InteresMe.API.Modules.Initiatives.Validators;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace InteresMe.API.Modules.Initiatives.Services.JoinRequests;

public sealed class InitiativeJoinRequestService(AppDbContext dbContext) : IInitiativeJoinRequestService
{
    private const int JoinRequestMessageMaxLength = 500;
    internal const int JoinRequestFieldMaxLength = 1000;
    private const string PostgresUniqueViolation = "23505";

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
                InitiativeErrorCodes.NotFound,
                "Initiative was not found.");
        }

        if (initiative.OwnerUserId == userId)
        {
            return ApplicationResult<InitiativeJoinRequestResponse>.Failure(
                ApplicationErrorKind.Conflict,
                InitiativeErrorCodes.ApplyOwnerForbidden,
                "Initiative owner cannot request to join their own initiative.");
        }

        if (initiative.Visibility != InitiativeVisibility.Public)
        {
            // Private initiatives are invisible to non-members: do not reveal that they exist.
            return ApplicationResult<InitiativeJoinRequestResponse>.Failure(
                ApplicationErrorKind.NotFound,
                InitiativeErrorCodes.NotFound,
                "Initiative was not found.");
        }

        if (initiative.Status is InitiativeStatus.Completed or InitiativeStatus.Archived)
        {
            return ApplicationResult<InitiativeJoinRequestResponse>.Failure(
                ApplicationErrorKind.Conflict,
                InitiativeErrorCodes.ApplyClosed,
                "This initiative is no longer accepting join requests.");
        }

        var normalized = NormalizeJoinRequest(request);

        if (normalized.Message?.Length > JoinRequestMessageMaxLength)
        {
            return ApplicationResult<InitiativeJoinRequestResponse>.Failure(
                ApplicationErrorKind.Validation,
                InitiativeErrorCodes.ApplyMessageTooLong,
                $"Message must be at most {JoinRequestMessageMaxLength} characters.");
        }

        var tooLongField = new (string Name, string? Value)[]
            {
                ("Motivation", normalized.Motivation),
                ("Experience", normalized.Experience),
                ("Contribution", normalized.Contribution),
                ("Availability", normalized.Availability)
            }
            .FirstOrDefault(field => field.Value?.Length > JoinRequestFieldMaxLength);

        if (tooLongField.Name is not null)
        {
            return ApplicationResult<InitiativeJoinRequestResponse>.Failure(
                ApplicationErrorKind.Validation,
                InitiativeErrorCodes.ApplyFieldTooLong,
                $"{tooLongField.Name} must be at most {JoinRequestFieldMaxLength} characters.");
        }

        if (normalized.RoleId is not null)
        {
            var roleBelongsToInitiative = await dbContext.InitiativeRoles
                .AnyAsync(
                    role =>
                        role.Id == normalized.RoleId.Value &&
                        role.InitiativeId == initiativeId,
                    cancellationToken);

            if (!roleBelongsToInitiative)
            {
                return ApplicationResult<InitiativeJoinRequestResponse>.Failure(
                    ApplicationErrorKind.Validation,
                    InitiativeErrorCodes.ApplyRoleInvalid,
                    "Role must belong to the initiative.");
            }
        }

        // An "active" request is pending or accepted; rejected requests may be re-submitted.
        var activeStatus = await dbContext.InitiativeJoinRequests
            .AsNoTracking()
            .Where(joinRequest =>
                joinRequest.InitiativeId == initiativeId &&
                joinRequest.UserId == userId &&
                (joinRequest.Status == InitiativeJoinRequestStatus.Pending ||
                 joinRequest.Status == InitiativeJoinRequestStatus.Accepted))
            .Select(joinRequest => (InitiativeJoinRequestStatus?)joinRequest.Status)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeStatus is not null)
        {
            return ActiveRequestConflict(activeStatus.Value);
        }

        var now = DateTime.UtcNow;
        var created = new InitiativeJoinRequest
        {
            Id = Guid.NewGuid(),
            InitiativeId = initiativeId,
            UserId = userId,
            RoleId = normalized.RoleId,
            Message = normalized.Message,
            Motivation = normalized.Motivation,
            Experience = normalized.Experience,
            Contribution = normalized.Contribution,
            Availability = normalized.Availability,
            Status = InitiativeJoinRequestStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.InitiativeJoinRequests.Add(created);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException { SqlState: PostgresUniqueViolation })
        {
            // A concurrent request created an active request first (partial unique index).
            return ActiveRequestConflict(InitiativeJoinRequestStatus.Pending);
        }

        return ApplicationResult<InitiativeJoinRequestResponse>.Success(
            InitiativeMapper.ToJoinRequestResponse(created));
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
                accessError.Value.Code,
                accessError.Value.Message);
        }

        var requests = await dbContext.InitiativeJoinRequests
            .AsNoTracking()
            .Include(joinRequest => joinRequest.User)
                .ThenInclude(user => user.Profile)
            .Where(joinRequest => joinRequest.InitiativeId == initiativeId)
            .OrderByDescending(joinRequest => joinRequest.CreatedAt)
            .ToListAsync(cancellationToken);

        return ApplicationResult<List<InitiativeJoinRequestResponse>>.Success(
            requests.Select(InitiativeMapper.ToJoinRequestResponse).ToList());
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
                accessError.Value.Code,
                accessError.Value.Message);
        }

        var joinRequest = await dbContext.InitiativeJoinRequests
            .Include(currentRequest => currentRequest.User)
                .ThenInclude(user => user.Profile)
            .FirstOrDefaultAsync(
                currentRequest =>
                    currentRequest.Id == requestId &&
                    currentRequest.InitiativeId == initiativeId,
                cancellationToken);

        if (joinRequest is null)
        {
            return ApplicationResult<InitiativeJoinRequestResponse>.Failure(
                ApplicationErrorKind.NotFound,
                InitiativeErrorCodes.ApplicationNotFound,
                "Join request was not found.");
        }

        if (joinRequest.Status != InitiativeJoinRequestStatus.Pending)
        {
            return ApplicationResult<InitiativeJoinRequestResponse>.Failure(
                ApplicationErrorKind.Conflict,
                InitiativeErrorCodes.ApplicationConflict,
                "Only pending join requests can be updated.");
        }

        if (status == InitiativeJoinRequestStatus.Accepted)
        {
            var initiative = await dbContext.Initiatives
                .AsNoTracking()
                .Where(currentInitiative => currentInitiative.Id == initiativeId)
                .Select(currentInitiative => new { currentInitiative.TeamSize })
                .FirstAsync(cancellationToken);

            if (initiative.TeamSize is int teamSize)
            {
                var acceptedCount = await dbContext.InitiativeJoinRequests
                    .CountAsync(
                        currentRequest =>
                            currentRequest.InitiativeId == initiativeId &&
                            currentRequest.Status == InitiativeJoinRequestStatus.Accepted,
                        cancellationToken);

                // The owner occupies one place in the team.
                if (acceptedCount + 1 >= teamSize)
                {
                    return ApplicationResult<InitiativeJoinRequestResponse>.Failure(
                        ApplicationErrorKind.Conflict,
                        InitiativeErrorCodes.TeamFull,
                        "The team is already full.");
                }
            }
        }

        joinRequest.Status = status;
        joinRequest.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return ApplicationResult<InitiativeJoinRequestResponse>.Success(
            InitiativeMapper.ToJoinRequestResponse(joinRequest));
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
                InitiativeErrorCodes.NotFound,
                "Initiative was not found.");
        }

        if (initiative.OwnerUserId != ownerUserId)
        {
            return new OwnerAccessError(
                ApplicationErrorKind.Forbidden,
                InitiativeErrorCodes.OwnerRequired,
                "Only the initiative owner can manage join requests.");
        }

        return null;
    }

    private static ApplicationResult<InitiativeJoinRequestResponse> ActiveRequestConflict(
        InitiativeJoinRequestStatus status) =>
        status == InitiativeJoinRequestStatus.Accepted
            ? ApplicationResult<InitiativeJoinRequestResponse>.Failure(
                ApplicationErrorKind.Conflict,
                InitiativeErrorCodes.ApplyAlreadyMember,
                "You are already a member of this initiative.")
            : ApplicationResult<InitiativeJoinRequestResponse>.Failure(
                ApplicationErrorKind.Conflict,
                InitiativeErrorCodes.ApplyAlreadyExists,
                "You already have a pending join request for this initiative.");

    private static NormalizedJoinRequest NormalizeJoinRequest(
        CreateInitiativeJoinRequestRequest request) => new(
        request.RoleId,
        InitiativeRequestNormalizer.NormalizeOptional(request.Message),
        InitiativeRequestNormalizer.NormalizeOptional(request.Motivation),
        InitiativeRequestNormalizer.NormalizeOptional(request.Experience),
        InitiativeRequestNormalizer.NormalizeOptional(request.Contribution),
        InitiativeRequestNormalizer.NormalizeOptional(request.Availability));

    private sealed record NormalizedJoinRequest(
        Guid? RoleId,
        string? Message,
        string? Motivation,
        string? Experience,
        string? Contribution,
        string? Availability);

    private readonly record struct OwnerAccessError(
        ApplicationErrorKind Kind,
        string Code,
        string Message);
}

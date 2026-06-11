using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.Data;
using InteresMe.API.Modules.Initiatives.Contracts.Requests;
using InteresMe.API.Modules.Initiatives.Contracts.Responses;
using InteresMe.API.Modules.Initiatives.Domain.Entities;
using InteresMe.API.Modules.Initiatives.Domain.Enums;
using InteresMe.API.Modules.Initiatives.Mapping;
using InteresMe.API.Modules.Initiatives.Validators;
using Microsoft.EntityFrameworkCore;

namespace InteresMe.API.Modules.Initiatives.Services.JoinRequests;

public sealed class InitiativeJoinRequestService(AppDbContext dbContext) : IInitiativeJoinRequestService
{
    private const int JoinRequestMessageMaxLength = 500;

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

        var normalized = NormalizeJoinRequest(request);

        if (normalized.Message?.Length > JoinRequestMessageMaxLength)
        {
            return ApplicationResult<InitiativeJoinRequestResponse>.Failure(
                ApplicationErrorKind.Validation,
                $"Message must be at most {JoinRequestMessageMaxLength} characters.");
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
                    "Role must belong to the initiative.");
            }
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
        await dbContext.SaveChangesAsync(cancellationToken);

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
                accessError.Value.Message);
        }

        var requests = await dbContext.InitiativeJoinRequests
            .AsNoTracking()
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
        string Message);
}

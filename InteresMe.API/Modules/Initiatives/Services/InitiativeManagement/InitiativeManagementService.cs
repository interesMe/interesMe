using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.Data;
using InteresMe.API.Modules.Initiatives.Contracts.Requests;
using InteresMe.API.Modules.Initiatives.Contracts.Responses;
using InteresMe.API.Modules.Initiatives.Domain.Entities;
using InteresMe.API.Modules.Initiatives.Domain.Enums;
using InteresMe.API.Modules.Initiatives.Mapping;
using InteresMe.API.Modules.Initiatives.Services.Slugs;
using InteresMe.API.Modules.Initiatives.Validators;
using Microsoft.EntityFrameworkCore;

namespace InteresMe.API.Modules.Initiatives.Services.InitiativeManagement;

public sealed class InitiativeManagementService(
    AppDbContext dbContext,
    IInitiativeRequestValidator initiativeRequestValidator,
    IInitiativeSlugService initiativeSlugService) : IInitiativeManagementService
{
    public async Task<ApplicationResult<InitiativeResponse>> CreateAsync(
        Guid ownerUserId,
        CreateInitiativeRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalized = InitiativeRequestNormalizer.Normalize(request);
        var validation = await initiativeRequestValidator.ValidateAsync(
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
            Slug = await initiativeSlugService.CreateUniqueSlugAsync(
                normalized.Title,
                cancellationToken),
            ShortDescription = normalized.ShortDescription,
            GoalType = normalized.GoalType,
            University = normalized.University,
            TeamSize = normalized.TeamSize,
            Status = normalized.Status ?? InitiativeStatus.Idea,
            Visibility = normalized.Visibility ?? InitiativeVisibility.Public,
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

        return ApplicationResult<InitiativeResponse>.Success(
            InitiativeMapper.ToResponse(created));
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

        var normalized = InitiativeRequestNormalizer.Normalize(request);
        var validation = await initiativeRequestValidator.ValidateAsync(
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
        initiative.Visibility = normalized.Visibility ?? initiative.Visibility;
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

        return ApplicationResult<InitiativeResponse>.Success(
            InitiativeMapper.ToResponse(updated));
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

    private IQueryable<Initiative> BaseInitiativeQuery() =>
        dbContext.Initiatives
            .AsNoTracking()
            .AsSplitQuery()
            .Include(initiative => initiative.InitiativeInterests)
                .ThenInclude(initiativeInterest => initiativeInterest.Interest)
            .Include(initiative => initiative.Roles);

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
}

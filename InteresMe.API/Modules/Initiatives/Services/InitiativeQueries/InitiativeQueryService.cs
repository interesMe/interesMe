using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.Data;
using InteresMe.API.Modules.Initiatives.Contracts.Responses;
using InteresMe.API.Modules.Initiatives.Domain.Entities;
using InteresMe.API.Modules.Initiatives.Domain.Enums;
using InteresMe.API.Modules.Initiatives.Mapping;
using Microsoft.EntityFrameworkCore;

namespace InteresMe.API.Modules.Initiatives.Services.InitiativeQueries;

public sealed class InitiativeQueryService(AppDbContext dbContext) : IInitiativeQueryService
{
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
            .Select(InitiativeMapper.ToResponse)
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
            InitiativeMapper.ToResponse(initiative));
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
                "Initiative was not found.");
        }

        return ApplicationResult<PublicInitiativeResponse>.Success(initiative);
    }

    private IQueryable<Initiative> BaseInitiativeQuery() =>
        dbContext.Initiatives
            .AsNoTracking()
            .AsSplitQuery()
            .Include(initiative => initiative.InitiativeInterests)
                .ThenInclude(initiativeInterest => initiativeInterest.Interest)
            .Include(initiative => initiative.Roles);
}

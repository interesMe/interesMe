using System.Text;
using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.Data;
using InteresMe.API.Modules.Interests.DTOs;
using InteresMe.API.Modules.Interests.Infrastructure.Seed;
using InteresMe.API.Modules.Interests.Models;
using Microsoft.EntityFrameworkCore;

namespace InteresMe.API.Modules.Interests.Services;

public sealed class InterestService(AppDbContext dbContext) : IInterestService
{
    private const int NameMaxLength = 80;
    private const int SlugMaxLength = 100;

    public async Task<List<InterestResponse>> GetAllAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.Interests
            .AsNoTracking()
            .OrderBy(interest => interest.Name)
            .Select(interest => new InterestResponse
            {
                Id = interest.Id,
                CategoryId = interest.CategoryId,
                Name = interest.Name,
                Slug = interest.Slug,
                Description = interest.Description,
                Icon = interest.Icon,
                Color = interest.Color,
                PeopleCount = 0,
                InitiativesCount = 0,
                PortfolioCount = 0,
                CreatedAt = interest.CreatedAt
            })
            .ToListAsync(cancellationToken);

    public async Task<ApplicationResult<InterestResponse>> CreateAsync(
        CreateInterestRequest request,
        CancellationToken cancellationToken = default)
    {
        var name = request.Name?.Trim() ?? string.Empty;
        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? CreateSlug(name)
            : CreateSlug(request.Slug);

        if (string.IsNullOrWhiteSpace(name))
        {
            return ApplicationResult<InterestResponse>.Failure(
                ApplicationErrorKind.Validation,
                "Interest name is required.");
        }

        if (name.Length > NameMaxLength)
        {
            return ApplicationResult<InterestResponse>.Failure(
                ApplicationErrorKind.Validation,
                $"Interest name must be at most {NameMaxLength} characters.");
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            return ApplicationResult<InterestResponse>.Failure(
                ApplicationErrorKind.Validation,
                "Interest slug is required.");
        }

        if (slug.Length > SlugMaxLength)
        {
            return ApplicationResult<InterestResponse>.Failure(
                ApplicationErrorKind.Validation,
                $"Interest slug must be at most {SlugMaxLength} characters.");
        }

        var existing = await dbContext.Interests
            .AsNoTracking()
            .FirstOrDefaultAsync(
                interest => interest.Slug == slug,
                cancellationToken);

        if (existing is not null)
        {
            return ApplicationResult<InterestResponse>.Success(ToResponse(existing));
        }

        var interest = new Interest
        {
            Id = Guid.NewGuid(),
            CategoryId = InterestCatalogSeed.BuildCategoryId,
            Name = name,
            Slug = slug,
            Description = name,
            Icon = "IN",
            Color = "#14b8a6",
            SortOrder = 1000,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Interests.Add(interest);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ApplicationResult<InterestResponse>.Success(ToResponse(interest));
    }

    private static InterestResponse ToResponse(Interest interest) => new()
    {
        Id = interest.Id,
        CategoryId = interest.CategoryId,
        Name = interest.Name,
        Slug = interest.Slug,
        Description = interest.Description,
        Icon = interest.Icon,
        Color = interest.Color,
        PeopleCount = 0,
        InitiativesCount = 0,
        PortfolioCount = 0,
        CreatedAt = interest.CreatedAt
    };

    private static string CreateSlug(string value)
    {
        var builder = new StringBuilder();
        var previousWasDash = false;

        foreach (var character in value.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                previousWasDash = false;
                continue;
            }

            if (previousWasDash)
            {
                continue;
            }

            builder.Append('-');
            previousWasDash = true;
        }

        return builder.ToString().Trim('-');
    }
}

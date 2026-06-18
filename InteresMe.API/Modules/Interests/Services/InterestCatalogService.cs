using InteresMe.API.Data;
using InteresMe.API.Modules.Interests.DTOs;
using Microsoft.EntityFrameworkCore;

namespace InteresMe.API.Modules.Interests.Services;

public sealed class InterestCatalogService(AppDbContext dbContext) : IInterestCatalogService
{
    public async Task<InterestCatalogResponse> GetCatalogAsync(
        CancellationToken cancellationToken = default)
    {
        var categories = await dbContext.InterestCategories
            .AsNoTracking()
            .AsSplitQuery()
            .Where(category => category.IsActive)
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Name)
            .Select(category => new InterestCategoryResponse
            {
                Id = category.Id,
                Name = category.Name,
                Slug = category.Slug,
                Description = category.Description,
                Icon = category.Icon,
                Color = category.Color,
                PeopleCount = 0,
                InitiativesCount = 0,
                PortfolioCount = 0,
                Interests = category.Interests
                    .Where(interest => interest.IsActive)
                    .OrderBy(interest => interest.SortOrder)
                    .ThenBy(interest => interest.Name)
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
                        CreatedAt = interest.CreatedAt,
                        Subinterests = interest.Subinterests
                            .Where(subinterest => subinterest.IsActive)
                            .OrderBy(subinterest => subinterest.SortOrder)
                            .ThenBy(subinterest => subinterest.Name)
                            .Select(subinterest => new SubinterestResponse
                            {
                                Id = subinterest.Id,
                                InterestId = subinterest.InterestId,
                                Name = subinterest.Name,
                                Slug = subinterest.Slug,
                                Description = subinterest.Description,
                                PeopleCount = 0,
                                InitiativesCount = 0,
                                PortfolioCount = 0
                            })
                            .ToList()
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        return new InterestCatalogResponse
        {
            Categories = categories
        };
    }
}

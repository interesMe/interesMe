namespace InteresMe.API.Modules.Interests.DTOs;

public sealed class InterestCatalogResponse
{
    public IReadOnlyList<InterestCategoryResponse> Categories { get; set; } = [];
}

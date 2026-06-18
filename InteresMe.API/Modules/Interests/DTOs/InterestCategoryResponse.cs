namespace InteresMe.API.Modules.Interests.DTOs;

public sealed class InterestCategoryResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Icon { get; set; } = string.Empty;

    public string Color { get; set; } = string.Empty;

    public int PeopleCount { get; set; }

    public int InitiativesCount { get; set; }

    public int PortfolioCount { get; set; }

    public IReadOnlyList<InterestResponse> Interests { get; set; } = [];
}

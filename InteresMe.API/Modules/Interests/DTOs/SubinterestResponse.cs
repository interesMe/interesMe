namespace InteresMe.API.Modules.Interests.DTOs;

public sealed class SubinterestResponse
{
    public Guid Id { get; set; }

    public Guid InterestId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int PeopleCount { get; set; }

    public int InitiativesCount { get; set; }

    public int PortfolioCount { get; set; }
}

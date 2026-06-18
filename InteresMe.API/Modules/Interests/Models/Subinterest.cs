namespace InteresMe.API.Modules.Interests.Models;

public sealed class Subinterest
{
    public Guid Id { get; set; }

    public Guid InterestId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public Interest Interest { get; set; } = null!;
}

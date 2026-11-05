namespace InteresMe.API.Modules.Discovery.DTOs;

public sealed class PeopleSearchResponse
{
    public IReadOnlyList<Guid> InterestIds { get; set; } = [];

    public bool UsedMyInterests { get; set; }

    /// <summary>Applied skill filters (lower-cased).</summary>
    public IReadOnlyList<string> SkillFilters { get; set; } = [];

    /// <summary>Skills of people matching the interests, offered as filter options.</summary>
    public IReadOnlyList<string> AvailableSkills { get; set; } = [];

    public IReadOnlyList<PersonMatchResponse> Items { get; set; } = [];
}

public sealed class PersonMatchResponse
{
    public Guid UserId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string? Headline { get; set; }

    public string? City { get; set; }

    public string? AvatarUrl { get; set; }

    public int SharedInterestCount { get; set; }

    public IReadOnlyList<SharedInterestResponse> SharedInterests { get; set; } = [];

    public IReadOnlyList<string> Skills { get; set; } = [];
}

public sealed class SharedInterestResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Color { get; set; } = string.Empty;
}

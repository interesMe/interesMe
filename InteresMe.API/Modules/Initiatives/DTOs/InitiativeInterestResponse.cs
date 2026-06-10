namespace InteresMe.API.Modules.Initiatives.DTOs;

public sealed class InitiativeInterestResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;
}

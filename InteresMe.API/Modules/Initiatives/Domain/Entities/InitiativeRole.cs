namespace InteresMe.API.Modules.Initiatives.Domain.Entities;

public sealed class InitiativeRole
{
    public Guid Id { get; set; }

    public Guid InitiativeId { get; set; }

    public string Name { get; set; } = string.Empty;

    public Initiative Initiative { get; set; } = null!;
}

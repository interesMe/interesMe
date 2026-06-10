using InteresMe.API.Modules.Interests.Models;

namespace InteresMe.API.Modules.Initiatives.Models;

public sealed class InitiativeInterest
{
    public Guid InitiativeId { get; set; }

    public Guid InterestId { get; set; }

    public Initiative Initiative { get; set; } = null!;

    public Interest Interest { get; set; } = null!;
}

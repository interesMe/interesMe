namespace InteresMe.API.Modules.Initiatives.Contracts.Requests;

public sealed class CreateInitiativeJoinRequestRequest
{
    public Guid? RoleId { get; set; }

    public string? Message { get; set; }

    public string? Motivation { get; set; }

    public string? Experience { get; set; }

    public string? Contribution { get; set; }

    public string? Availability { get; set; }
}

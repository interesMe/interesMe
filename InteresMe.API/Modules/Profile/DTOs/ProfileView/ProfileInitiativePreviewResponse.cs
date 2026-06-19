using InteresMe.API.Modules.Initiatives.Domain.Enums;

namespace InteresMe.API.Modules.Profile.DTOs.ProfileView;

public sealed class ProfileInitiativePreviewResponse
{
    public Guid Id { get; set; }

    public string Slug { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string ShortDescription { get; set; } = string.Empty;

    public InitiativeGoalType GoalType { get; set; }

    public InitiativeStatus Status { get; set; }

    public InitiativeVisibility Visibility { get; set; }

    public DateTime CreatedAt { get; set; }
}

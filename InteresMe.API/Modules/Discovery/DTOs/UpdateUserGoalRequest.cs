using InteresMe.API.Modules.Discovery.Models;

namespace InteresMe.API.Modules.Discovery.DTOs;

public sealed class UpdateUserGoalRequest
{
    public UserGoal Goal { get; set; }
}

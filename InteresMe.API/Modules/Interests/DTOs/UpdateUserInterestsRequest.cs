namespace InteresMe.API.Modules.Interests.DTOs;

public class UpdateUserInterestsRequest
{
    public List<Guid> InterestIds { get; set; } = [];
}

namespace InteresMe.API.BuildingBlocks.Security;

public interface ICurrentUser
{
    Guid UserId { get; }
}

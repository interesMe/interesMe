using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.Modules.Profile.DTOs.ProfileView;

namespace InteresMe.API.Modules.Profile.Services;

public interface IProfileViewService
{
    Task<ApplicationResult<ProfileViewResponse>> GetAsync(
        Guid viewerUserId,
        Guid profileUserId,
        CancellationToken cancellationToken = default);

    Task<ApplicationResult<bool>> FollowAsync(
        Guid followerId,
        Guid followedId,
        CancellationToken cancellationToken = default);

    Task<ApplicationResult<bool>> UnfollowAsync(
        Guid followerId,
        Guid followedId,
        CancellationToken cancellationToken = default);
}

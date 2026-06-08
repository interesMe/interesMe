using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.Modules.Interests.DTOs;
using InteresMe.API.Modules.Profile.DTOs;

namespace InteresMe.API.Modules.Profile.Services;

public interface IProfileService
{
    Task<ApplicationResult<ProfileResponse>> GetMyProfileAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<ApplicationResult<ProfileResponse>> CreateMyProfileAsync(
        Guid userId,
        CreateProfileRequest request,
        CancellationToken cancellationToken = default);

    Task<ApplicationResult<ProfileResponse>> CreateMyProfileWithAvatarAsync(
        Guid userId,
        CreateProfileRequest request,
        IFormFile? avatarFile,
        CancellationToken cancellationToken = default);

    Task<ApplicationResult<ProfileResponse>> UpdateMyProfileAsync(
        Guid userId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default);

    Task<ApplicationResult<ProfileResponse>> UpdateMyProfileWithAvatarAsync(
        Guid userId,
        UpdateProfileRequest request,
        IFormFile? avatarFile,
        CancellationToken cancellationToken = default);

    Task<ApplicationResult<ProfileResponse>> UploadMyAvatarAsync(
        Guid userId,
        IFormFile file,
        CancellationToken cancellationToken = default);

    Task<ApplicationResult<List<InterestResponse>>> ReplaceMyInterestsAsync(
        Guid userId,
        UpdateUserInterestsRequest request,
        CancellationToken cancellationToken = default);
}

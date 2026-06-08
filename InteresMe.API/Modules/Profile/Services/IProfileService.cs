using InteresMe.API.Modules.Auth.Services;
using InteresMe.API.Modules.Interests.DTOs;
using InteresMe.API.Modules.Profile.DTOs;

namespace InteresMe.API.Modules.Profile.Services;

public interface IProfileService
{
    Task<AuthResult<ProfileResponse>> GetMyProfileAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<AuthResult<ProfileResponse>> CreateMyProfileAsync(
        Guid userId,
        CreateProfileRequest request,
        CancellationToken cancellationToken = default);

    Task<AuthResult<ProfileResponse>> CreateMyProfileWithAvatarAsync(
        Guid userId,
        CreateProfileRequest request,
        IFormFile? avatarFile,
        CancellationToken cancellationToken = default);

    Task<AuthResult<ProfileResponse>> UpdateMyProfileAsync(
        Guid userId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default);

    Task<AuthResult<ProfileResponse>> UpdateMyProfileWithAvatarAsync(
        Guid userId,
        UpdateProfileRequest request,
        IFormFile? avatarFile,
        CancellationToken cancellationToken = default);

    Task<AuthResult<ProfileResponse>> UploadMyAvatarAsync(
        Guid userId,
        IFormFile file,
        CancellationToken cancellationToken = default);

    Task<AuthResult<List<InterestResponse>>> ReplaceMyInterestsAsync(
        Guid userId,
        UpdateUserInterestsRequest request,
        CancellationToken cancellationToken = default);
}

using InteresMe.API.Data;
using InteresMe.API.Modules.Auth.Services;
using InteresMe.API.Modules.Interests.DTOs;
using InteresMe.API.Modules.Interests.Models;
using InteresMe.API.Modules.Profile.DTOs;
using InteresMe.API.Modules.Profile.Models;
using Microsoft.EntityFrameworkCore;

namespace InteresMe.API.Modules.Profile.Services;

public sealed class ProfileService(AppDbContext dbContext) : IProfileService
{
    private const int DisplayNameMaxLength = 80;
    private const int CityMaxLength = 120;
    private const int AvatarUrlMaxLength = 2048;

    public async Task<AuthResult<ProfileResponse>> GetMyProfileAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var profile = await dbContext.UserProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(
                userProfile => userProfile.UserId == userId,
                cancellationToken);

        if (profile is null)
        {
            return AuthResult<ProfileResponse>.Failure(
                AuthErrorKind.InvalidCredentials,
                "Profile was not found.");
        }

        return AuthResult<ProfileResponse>.Success(ToProfileResponse(profile));
    }

    public async Task<AuthResult<ProfileResponse>> CreateMyProfileAsync(
        Guid userId,
        CreateProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateProfile(
            request.DisplayName,
            request.City,
            request.AvatarUrl,
            request.BirthDate);

        if (validation is not null)
        {
            return AuthResult<ProfileResponse>.Failure(
                AuthErrorKind.Validation,
                validation);
        }

        var profileExists = await dbContext.UserProfiles
            .AnyAsync(
                userProfile => userProfile.UserId == userId,
                cancellationToken);

        if (profileExists)
        {
            return AuthResult<ProfileResponse>.Failure(
                AuthErrorKind.EmailAlreadyExists,
                "Profile already exists.");
        }

        var userExists = await dbContext.Users
            .AnyAsync(
                user => user.Id == userId,
                cancellationToken);

        if (!userExists)
        {
            return AuthResult<ProfileResponse>.Failure(
                AuthErrorKind.InvalidCredentials,
                "User was not found.");
        }

        var now = DateTime.UtcNow;
        var profile = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DisplayName = request.DisplayName.Trim(),
            City = NormalizeOptional(request.City),
            AvatarUrl = NormalizeOptional(request.AvatarUrl),
            BirthDate = request.BirthDate,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.UserProfiles.Add(profile);
        await dbContext.SaveChangesAsync(cancellationToken);

        return AuthResult<ProfileResponse>.Success(ToProfileResponse(profile));
    }

    public async Task<AuthResult<ProfileResponse>> UpdateMyProfileAsync(
        Guid userId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateProfile(
            request.DisplayName,
            request.City,
            request.AvatarUrl,
            request.BirthDate);

        if (validation is not null)
        {
            return AuthResult<ProfileResponse>.Failure(
                AuthErrorKind.Validation,
                validation);
        }

        var profile = await dbContext.UserProfiles
            .FirstOrDefaultAsync(
                userProfile => userProfile.UserId == userId,
                cancellationToken);

        if (profile is null)
        {
            return AuthResult<ProfileResponse>.Failure(
                AuthErrorKind.InvalidCredentials,
                "Profile was not found.");
        }

        profile.DisplayName = request.DisplayName.Trim();
        profile.City = NormalizeOptional(request.City);
        profile.AvatarUrl = NormalizeOptional(request.AvatarUrl);
        profile.BirthDate = request.BirthDate;
        profile.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return AuthResult<ProfileResponse>.Success(ToProfileResponse(profile));
    }

    public async Task<AuthResult<List<InterestResponse>>> ReplaceMyInterestsAsync(
        Guid userId,
        UpdateUserInterestsRequest request,
        CancellationToken cancellationToken = default)
    {
        var interestIds = request.InterestIds
            .Distinct()
            .ToList();

        if (interestIds.Count == 0)
        {
            await dbContext.UserInterests
                .Where(userInterest => userInterest.UserId == userId)
                .ExecuteDeleteAsync(cancellationToken);

            return AuthResult<List<InterestResponse>>.Success([]);
        }

        var interests = await dbContext.Interests
            .Where(interest => interestIds.Contains(interest.Id))
            .OrderBy(interest => interest.Name)
            .ToListAsync(cancellationToken);

        if (interests.Count != interestIds.Count)
        {
            return AuthResult<List<InterestResponse>>.Failure(
                AuthErrorKind.Validation,
                "One or more interests do not exist.");
        }

        await dbContext.UserInterests
            .Where(userInterest => userInterest.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var userInterests = interestIds.Select(interestId => new UserInterest
        {
            UserId = userId,
            InterestId = interestId,
            CreatedAt = now
        });

        dbContext.UserInterests.AddRange(userInterests);
        await dbContext.SaveChangesAsync(cancellationToken);

        return AuthResult<List<InterestResponse>>.Success(
            interests.Select(ToInterestResponse).ToList());
    }

    private static string? ValidateProfile(
        string displayName,
        string? city,
        string? avatarUrl,
        DateOnly? birthDate)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return "Display name is required.";
        }

        if (displayName.Trim().Length > DisplayNameMaxLength)
        {
            return $"Display name must be at most {DisplayNameMaxLength} characters.";
        }

        if (!string.IsNullOrWhiteSpace(city) &&
            city.Trim().Length > CityMaxLength)
        {
            return $"City must be at most {CityMaxLength} characters.";
        }

        if (!string.IsNullOrWhiteSpace(avatarUrl) &&
            avatarUrl.Trim().Length > AvatarUrlMaxLength)
        {
            return $"Avatar URL must be at most {AvatarUrlMaxLength} characters.";
        }

        if (birthDate is not null &&
            birthDate.Value > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return "Birth date cannot be in the future.";
        }

        return null;
    }

    private static ProfileResponse ToProfileResponse(UserProfile profile) => new()
    {
        Id = profile.Id,
        UserId = profile.UserId,
        DisplayName = profile.DisplayName,
        City = profile.City,
        AvatarUrl = profile.AvatarUrl,
        BirthDate = profile.BirthDate,
        CreatedAt = profile.CreatedAt,
        UpdatedAt = profile.UpdatedAt
    };

    private static InterestResponse ToInterestResponse(Interest interest) => new()
    {
        Id = interest.Id,
        Name = interest.Name,
        Slug = interest.Slug,
        CreatedAt = interest.CreatedAt
    };

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();

        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}

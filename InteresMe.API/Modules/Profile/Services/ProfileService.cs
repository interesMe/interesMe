using System.Text.Json;
using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.Data;
using InteresMe.API.Modules.History.DTOs;
using InteresMe.API.Modules.History.Services;
using InteresMe.API.Modules.Interests.DTOs;
using InteresMe.API.Modules.Interests.Models;
using InteresMe.API.Modules.Profile.DTOs;
using InteresMe.API.Modules.Profile.Models;
using Microsoft.EntityFrameworkCore;

namespace InteresMe.API.Modules.Profile.Services;

public sealed class ProfileService(
    AppDbContext dbContext,
    IUserHistoryService userHistoryService,
    IWebHostEnvironment webHostEnvironment) : IProfileService
{
    private const int DisplayNameMaxLength = 80;
    private const int HeadlineMaxLength = 120;
    private const int BioMaxLength = 500;
    private const int CityMaxLength = 120;
    private const int AvatarUrlMaxLength = 2048;
    private const long AvatarMaxSizeBytes = 2 * 1024 * 1024;
    private const string AvatarUploadPathPrefix = "/uploads/avatars/";
    private static readonly Dictionary<string, string> AllowedAvatarExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp"
    };

    public async Task<ApplicationResult<ProfileResponse>> GetMyProfileAsync(
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
            return ApplicationResult<ProfileResponse>.Failure(
                ApplicationErrorKind.NotFound,
                "Profile was not found.");
        }

        return ApplicationResult<ProfileResponse>.Success(ToProfileResponse(profile));
    }

    public async Task<ApplicationResult<ProfileResponse>> CreateMyProfileAsync(
        Guid userId,
        CreateProfileRequest request,
        CancellationToken cancellationToken = default) =>
        await CreateMyProfileWithAvatarAsync(
            userId,
            request,
            avatarFile: null,
            cancellationToken);

    public async Task<ApplicationResult<ProfileResponse>> CreateMyProfileWithAvatarAsync(
        Guid userId,
        CreateProfileRequest request,
        IFormFile? avatarFile,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateProfile(
            request.DisplayName,
            request.Headline,
            request.Bio,
            request.City,
            request.AvatarUrl,
            request.BirthDate);

        if (validation is not null)
        {
            return ApplicationResult<ProfileResponse>.Failure(
                ApplicationErrorKind.Validation,
                validation);
        }

        var profileExists = await dbContext.UserProfiles
            .AnyAsync(
                userProfile => userProfile.UserId == userId,
                cancellationToken);

        if (profileExists)
        {
            return ApplicationResult<ProfileResponse>.Failure(
                ApplicationErrorKind.Conflict,
                "Profile already exists.");
        }

        var userExists = await dbContext.Users
            .AnyAsync(
                user => user.Id == userId,
                cancellationToken);

        if (!userExists)
        {
            return ApplicationResult<ProfileResponse>.Failure(
                ApplicationErrorKind.NotFound,
                "User was not found.");
        }

        if (avatarFile is not null)
        {
            var avatarValidation = ValidateAvatarFile(avatarFile);

            if (avatarValidation is not null)
            {
                return ApplicationResult<ProfileResponse>.Failure(
                    ApplicationErrorKind.Validation,
                    avatarValidation);
            }
        }

        var now = DateTime.UtcNow;
        var profile = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DisplayName = request.DisplayName.Trim(),
            Headline = NormalizeOptional(request.Headline),
            Bio = NormalizeOptional(request.Bio),
            City = NormalizeOptional(request.City),
            AvatarUrl = NormalizeOptional(request.AvatarUrl),
            BirthDate = request.BirthDate,
            CreatedAt = now,
            UpdatedAt = now
        };

        if (avatarFile is not null)
        {
            profile.AvatarUrl = await SaveAvatarFileAsync(
                userId,
                avatarFile,
                cancellationToken);
        }

        dbContext.UserProfiles.Add(profile);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ApplicationResult<ProfileResponse>.Success(ToProfileResponse(profile));
    }

    public async Task<ApplicationResult<ProfileResponse>> UpdateMyProfileAsync(
        Guid userId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default) =>
        await UpdateMyProfileWithAvatarAsync(
            userId,
            request,
            avatarFile: null,
            cancellationToken);

    public async Task<ApplicationResult<ProfileResponse>> UpdateMyProfileWithAvatarAsync(
        Guid userId,
        UpdateProfileRequest request,
        IFormFile? avatarFile,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateProfile(
            request.DisplayName,
            request.Headline,
            request.Bio,
            request.City,
            request.AvatarUrl,
            request.BirthDate);

        if (validation is not null)
        {
            return ApplicationResult<ProfileResponse>.Failure(
                ApplicationErrorKind.Validation,
                validation);
        }

        var profile = await dbContext.UserProfiles
            .FirstOrDefaultAsync(
                userProfile => userProfile.UserId == userId,
                cancellationToken);

        if (profile is null)
        {
            return ApplicationResult<ProfileResponse>.Failure(
                ApplicationErrorKind.NotFound,
                "Profile was not found.");
        }

        if (avatarFile is not null)
        {
            var avatarValidation = ValidateAvatarFile(avatarFile);

            if (avatarValidation is not null)
            {
                return ApplicationResult<ProfileResponse>.Failure(
                    ApplicationErrorKind.Validation,
                    avatarValidation);
            }
        }

        var previousAvatarUrl = profile.AvatarUrl;

        profile.DisplayName = request.DisplayName.Trim();
        profile.Headline = NormalizeOptional(request.Headline);
        profile.Bio = NormalizeOptional(request.Bio);
        profile.City = NormalizeOptional(request.City);
        profile.AvatarUrl = avatarFile is null
            ? NormalizeOptional(request.AvatarUrl)
            : await SaveAvatarFileAsync(userId, avatarFile, cancellationToken);
        profile.BirthDate = request.BirthDate;
        profile.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        if (avatarFile is not null)
        {
            DeletePreviousLocalAvatar(previousAvatarUrl, GetWebRootPath());
        }

        return ApplicationResult<ProfileResponse>.Success(ToProfileResponse(profile));
    }

    public async Task<ApplicationResult<ProfileResponse>> UploadMyAvatarAsync(
        Guid userId,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateAvatarFile(file);

        if (validation is not null)
        {
            return ApplicationResult<ProfileResponse>.Failure(
                ApplicationErrorKind.Validation,
                validation);
        }

        var profile = await dbContext.UserProfiles
            .FirstOrDefaultAsync(
                userProfile => userProfile.UserId == userId,
                cancellationToken);

        if (profile is null)
        {
            return ApplicationResult<ProfileResponse>.Failure(
                ApplicationErrorKind.NotFound,
                "Profile was not found.");
        }

        var webRootPath = GetWebRootPath();
        var previousAvatarUrl = profile.AvatarUrl;
        profile.AvatarUrl = await SaveAvatarFileAsync(
            userId,
            file,
            cancellationToken);
        profile.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        DeletePreviousLocalAvatar(previousAvatarUrl, webRootPath);

        return ApplicationResult<ProfileResponse>.Success(ToProfileResponse(profile));
    }

    public async Task<ApplicationResult<List<InterestResponse>>> ReplaceMyInterestsAsync(
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

            return ApplicationResult<List<InterestResponse>>.Success([]);
        }

        var existingInterestIds = await dbContext.UserInterests
            .AsNoTracking()
            .Where(userInterest => userInterest.UserId == userId)
            .Select(userInterest => userInterest.InterestId)
            .ToListAsync(cancellationToken);

        var existingInterestIdSet = existingInterestIds.ToHashSet();

        var interests = await dbContext.Interests
            .Include(interest => interest.Category)
            .Where(interest => interestIds.Contains(interest.Id))
            .OrderBy(interest => interest.Name)
            .ToListAsync(cancellationToken);

        if (interests.Count != interestIds.Count)
        {
            return ApplicationResult<List<InterestResponse>>.Failure(
                ApplicationErrorKind.Validation,
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

        var addedInterests = interests
            .Where(interest => !existingInterestIdSet.Contains(interest.Id))
            .ToList();

        foreach (var interest in addedInterests)
        {
            var interestPath = $"{interest.Category.Name} → {interest.Name}";

            await userHistoryService.AddEventAsync(
                new AddUserHistoryEventRequest(
                    UserId: userId,
                    Type: HistoryEventTypes.AddedInterest,
                    Title: $"Added {interest.Name}",
                    Description: interestPath,
                    TargetType: "interest",
                    TargetId: interest.Id,
                    TargetName: interest.Name,
                    MetadataJson: JsonSerializer.Serialize(new
                    {
                        category = interest.Category.Name,
                        interestPath
                    })),
                cancellationToken);
        }

        return ApplicationResult<List<InterestResponse>>.Success(
            interests.Select(ToInterestResponse).ToList());
    }

    private static string? ValidateProfile(
        string displayName,
        string? headline,
        string? bio,
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

        if (!string.IsNullOrWhiteSpace(headline) &&
            headline.Trim().Length > HeadlineMaxLength)
        {
            return $"Headline must be at most {HeadlineMaxLength} characters.";
        }

        if (!string.IsNullOrWhiteSpace(bio) &&
            bio.Trim().Length > BioMaxLength)
        {
            return $"Bio must be at most {BioMaxLength} characters.";
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

    private async Task<string> SaveAvatarFileAsync(
        Guid userId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var avatarDirectory = Path.Combine(
            GetWebRootPath(),
            "uploads",
            "avatars",
            userId.ToString());

        Directory.CreateDirectory(avatarDirectory);

        var filePath = Path.Combine(avatarDirectory, fileName);

        await using var stream = File.Create(filePath);
        await file.CopyToAsync(stream, cancellationToken);

        return $"{AvatarUploadPathPrefix}{userId}/{fileName}";
    }

    private static string? ValidateAvatarFile(IFormFile file)
    {
        if (file.Length <= 0)
        {
            return "Avatar file is required.";
        }

        if (file.Length > AvatarMaxSizeBytes)
        {
            return "Avatar file must be 2 MB or smaller.";
        }

        var extension = Path.GetExtension(file.FileName);

        if (string.IsNullOrWhiteSpace(extension) ||
            !AllowedAvatarExtensions.TryGetValue(extension, out var expectedContentType))
        {
            return "Avatar file must be a JPG, PNG, or WEBP image.";
        }

        if (!string.Equals(file.ContentType, expectedContentType, StringComparison.OrdinalIgnoreCase))
        {
            return "Avatar file content type is not supported.";
        }

        return null;
    }

    private string GetWebRootPath()
    {
        if (!string.IsNullOrWhiteSpace(webHostEnvironment.WebRootPath))
        {
            return webHostEnvironment.WebRootPath;
        }

        return Path.Combine(webHostEnvironment.ContentRootPath, "wwwroot");
    }

    private static void DeletePreviousLocalAvatar(
        string? previousAvatarUrl,
        string webRootPath)
    {
        if (string.IsNullOrWhiteSpace(previousAvatarUrl) ||
            !previousAvatarUrl.StartsWith(AvatarUploadPathPrefix, StringComparison.Ordinal))
        {
            return;
        }

        var relativePath = previousAvatarUrl
            .TrimStart('/')
            .Replace('/', Path.DirectorySeparatorChar);
        var filePath = Path.GetFullPath(Path.Combine(webRootPath, relativePath));
        var rootPath = Path.GetFullPath(webRootPath);
        var rootPathWithSeparator = rootPath.EndsWith(Path.DirectorySeparatorChar)
            ? rootPath
            : $"{rootPath}{Path.DirectorySeparatorChar}";

        if (!filePath.StartsWith(rootPathWithSeparator, StringComparison.Ordinal))
        {
            return;
        }

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    private static ProfileResponse ToProfileResponse(UserProfile profile) => new()
    {
        Id = profile.Id,
        UserId = profile.UserId,
        DisplayName = profile.DisplayName,
        Headline = profile.Headline,
        Bio = profile.Bio,
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

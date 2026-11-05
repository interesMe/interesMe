using InteresMe.API.Modules.Auth.DTOs;
using InteresMe.API.Modules.Profile.DTOs;

namespace InteresMe.API.Tests.Infrastructure;

internal static class TestUsers
{
    /// <summary>Registers a user through the real AuthService and creates a profile for them.</summary>
    public static async Task<Guid> CreateWithProfileAsync(
        PostgresDatabase database,
        string displayName,
        List<string>? skills = null)
    {
        await using var context = database.CreateContext();
        var registration = await TestServices.CreateAuthService(context).RegisterAsync(new RegisterRequest
        {
            Email = $"{Guid.NewGuid():N}@example.com",
            DisplayName = displayName,
            Password = "password-123"
        });
        Assert.True(registration.IsSuccess, registration.ErrorMessage);

        var userId = registration.Response!.UserId;
        var profile = await TestServices.CreateProfileService(context).CreateMyProfileAsync(userId, new CreateProfileRequest
        {
            DisplayName = displayName,
            Skills = skills
        });
        Assert.True(profile.IsSuccess, profile.ErrorMessage);

        return userId;
    }
}

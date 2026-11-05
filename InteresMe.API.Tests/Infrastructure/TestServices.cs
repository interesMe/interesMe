using InteresMe.API.BuildingBlocks.Email;
using InteresMe.API.Data;
using InteresMe.API.Modules.Auth.Services;
using InteresMe.API.Modules.History.Services;
using InteresMe.API.Modules.Profile.Services;
using InteresMe.API.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;

namespace InteresMe.API.Tests.Infrastructure;

internal static class TestServices
{
    public static AuthService CreateAuthService(AppDbContext context) =>
        new(
            context,
            new JwtTokenService(),
            oAuthProviderService: null!,
            new MemoryCache(new MemoryCacheOptions()),
            new NoOpEmailService(),
            NullLogger<AuthService>.Instance);

    public static ProfileService CreateProfileService(AppDbContext context) =>
        new(context, new UserHistoryService(context), new TestWebHostEnvironment());

    private sealed class NoOpEmailService : IEmailService
    {
        public Task SendWelcomeEmailAsync(
            string email,
            string displayName,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task SendEmailVerificationAsync(
            string email,
            string displayName,
            string verificationToken,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ApplicationName { get; set; } = "InteresMe.API.Tests";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public string EnvironmentName { get; set; } = "Testing";
    }
}

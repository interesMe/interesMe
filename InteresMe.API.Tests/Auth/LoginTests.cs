using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.Modules.Auth.DTOs;
using InteresMe.API.Tests.Infrastructure;

namespace InteresMe.API.Tests.Auth;

/// <summary>IME-8: login acceptance criteria.</summary>
public sealed class LoginTests(PostgresDatabase database) : IClassFixture<PostgresDatabase>
{
    private const string Password = "login-password-1";

    private async Task<string> RegisterAsync(string email)
    {
        await using var context = database.CreateContext();
        var result = await TestServices.CreateAuthService(context).RegisterAsync(new RegisterRequest
        {
            Email = email,
            DisplayName = "Login User",
            Password = Password
        });
        Assert.True(result.IsSuccess, result.ErrorMessage);
        return email;
    }

    [Fact]
    public async Task Login_WithCorrectCredentials_ReturnsJwtAndRefreshToken()
    {
        var email = await RegisterAsync("login-ok@example.com");
        await using var context = database.CreateContext();

        var result = await TestServices.CreateAuthService(context).LoginAsync(new LoginRequest
        {
            Email = "  LOGIN-OK@example.com ",
            Password = Password
        });

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal(email, result.Response!.Email);
        Assert.Equal(3, result.Response.Token.Split('.').Length);
        Assert.False(string.IsNullOrWhiteSpace(result.Response.RefreshToken));
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorizedWithClearMessage()
    {
        var email = await RegisterAsync("login-wrong@example.com");
        await using var context = database.CreateContext();

        var result = await TestServices.CreateAuthService(context).LoginAsync(new LoginRequest
        {
            Email = email,
            Password = "wrong-password"
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorKind.Unauthorized, result.ErrorKind);
        Assert.Equal("Invalid email or password.", result.ErrorMessage);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ReturnsSameMessageAsWrongPassword()
    {
        await using var context = database.CreateContext();

        var result = await TestServices.CreateAuthService(context).LoginAsync(new LoginRequest
        {
            Email = "nobody@example.com",
            Password = "whatever-123"
        });

        Assert.Equal(ApplicationErrorKind.Unauthorized, result.ErrorKind);
        Assert.Equal("Invalid email or password.", result.ErrorMessage);
    }

    [Fact]
    public async Task Login_WithEmptyFields_ReturnsValidationError()
    {
        await using var context = database.CreateContext();

        var result = await TestServices.CreateAuthService(context).LoginAsync(new LoginRequest());

        Assert.Equal(ApplicationErrorKind.Validation, result.ErrorKind);
    }

    [Fact]
    public async Task Session_RefreshToken_IssuesNewAccessToken()
    {
        var email = await RegisterAsync("login-refresh@example.com");
        await using var context = database.CreateContext();
        var service = TestServices.CreateAuthService(context);
        var login = await service.LoginAsync(new LoginRequest { Email = email, Password = Password });

        var refreshed = await service.RefreshTokenAsync(new RefreshRequest { RefreshToken = login.Response!.RefreshToken });

        Assert.True(refreshed.IsSuccess, refreshed.ErrorMessage);
    }
}

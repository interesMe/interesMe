using InteresMe.API.Data;
using InteresMe.API.Modules.Auth.DTOs;
using InteresMe.API.Modules.Auth.Models;
using InteresMe.API.Security;
using Microsoft.EntityFrameworkCore;

namespace InteresMe.API.Modules.Auth.Services;

public class AuthService(AppDbContext dbContext, JwtTokenService jwtTokenService) : IAuthService
{
    private const int MinPasswordLength = 8;

    public async Task<AuthResult> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        var displayName = request.DisplayName?.Trim() ?? string.Empty;
        var password = request.Password ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(displayName) ||
            string.IsNullOrWhiteSpace(password))
        {
            return AuthResult.Failure(
                AuthErrorKind.Validation,
                "Email, display name, and password are required.");
        }

        if (password.Length < MinPasswordLength)
        {
            return AuthResult.Failure(
                AuthErrorKind.Validation,
                $"Password must be at least {MinPasswordLength} characters.");
        }

        var emailExists = await dbContext.Users
            .AnyAsync(user => user.Email == email, cancellationToken);

        if (emailExists)
        {
            return AuthResult.Failure(
                AuthErrorKind.EmailAlreadyExists,
                "A user with this email already exists.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = displayName,
            PasswordHash = PasswordHasher.Hash(password),
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return AuthResult.Success(BuildAuthResponse(user));
    }

    public async Task<AuthResult> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        var password = request.Password ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return AuthResult.Failure(
                AuthErrorKind.Validation,
                "Email and password are required.");
        }

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null || !PasswordHasher.Verify(password, user.PasswordHash))
        {
            return AuthResult.Failure(
                AuthErrorKind.InvalidCredentials,
                "Invalid email or password.");
        }

        return AuthResult.Success(BuildAuthResponse(user));
    }

    private AuthResponse BuildAuthResponse(User user) => new()
    {
        UserId = user.Id,
        Email = user.Email,
        DisplayName = user.DisplayName,
        Token = jwtTokenService.CreateToken(user)
    };
}

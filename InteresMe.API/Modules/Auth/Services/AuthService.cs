using System.Data;
using InteresMe.API.Data;
using InteresMe.API.Modules.Auth.DTOs;
using InteresMe.API.Modules.Auth.Models;
using InteresMe.API.Security;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace InteresMe.API.Modules.Auth.Services;

public class AuthService(
    AppDbContext dbContext,
    JwtTokenService jwtTokenService,
    IOAuthProviderService oAuthProviderService)
    : IAuthService
{
    private const int MinPasswordLength = 8;
    private const int RefreshTokenLifetimeDays = 7;
    private const string PostgresUniqueViolation = "23505";

    public async Task<AuthResult<AuthResponse>> RegisterAsync(
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
            return AuthResult<AuthResponse>.Failure(
                AuthErrorKind.Validation,
                "Email, display name, and password are required.");
        }

        if (password.Length < MinPasswordLength)
        {
            return AuthResult<AuthResponse>.Failure(
                AuthErrorKind.Validation,
                $"Password must be at least {MinPasswordLength} characters.");
        }

        var emailExists = await dbContext.Users
            .AnyAsync(user => user.Email == email, cancellationToken);

        if (emailExists)
        {
            return AuthResult<AuthResponse>.Failure(
                AuthErrorKind.EmailAlreadyExists,
                "A user with this email already exists.");
        }

        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = displayName,
            PasswordHash = PasswordHasher.Hash(password),
            CreatedAt = now
        };

        var (refreshToken, refreshTokenEntity) = CreateRefreshToken(user.Id, now);

        dbContext.Users.Add(user);
        dbContext.Set<RefreshToken>().Add(refreshTokenEntity);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            return AuthResult<AuthResponse>.Failure(
                AuthErrorKind.EmailAlreadyExists,
                "A user with this email already exists.");
        }

        return AuthResult<AuthResponse>.Success(
            BuildAuthResponse(user, refreshToken));
    }

    public async Task<AuthResult<AuthResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        var password = request.Password ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            return AuthResult<AuthResponse>.Failure(
                AuthErrorKind.Validation,
                "Email and password are required.");
        }

        var user = await dbContext.Users
            .FirstOrDefaultAsync(
                u => u.Email == email,
                cancellationToken);

        if (user is null ||
            !PasswordHasher.Verify(password, user.PasswordHash))
        {
            return AuthResult<AuthResponse>.Failure(
                AuthErrorKind.InvalidCredentials,
                "Invalid email or password.");
        }

        var refreshToken = await CreateRefreshTokenAsync(
            user.Id,
            cancellationToken);

        return AuthResult<AuthResponse>.Success(
            BuildAuthResponse(user, refreshToken));
    }

    public async Task<AuthResult<RefreshResponse>> RefreshTokenAsync(
        RefreshRequest request,
        CancellationToken cancellationToken = default)
    {
        var requestRefreshToken = request.RefreshToken?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(requestRefreshToken))
        {
            return AuthResult<RefreshResponse>.Failure(
                AuthErrorKind.Validation,
                "Refresh token is required.");
        }

        var tokenHash = RefreshTokenHasher.HashToken(requestRefreshToken);
        var now = DateTime.UtcNow;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);

        var refreshToken = await dbContext.Set<RefreshToken>()
            .Include(rt => rt.user)
            .FirstOrDefaultAsync(
                rt => rt.TokenHash == tokenHash,
                cancellationToken);

        if (refreshToken is null ||
            refreshToken.ExpiresAt <= now ||
            refreshToken.RevokedAt != null)
        {
            return AuthResult<RefreshResponse>.Failure(
                AuthErrorKind.InvalidCredentials,
                "Invalid or expired refresh token.");
        }

        var revokedRows = await dbContext.Set<RefreshToken>()
            .Where(rt =>
                rt.Id == refreshToken.Id &&
                rt.RevokedAt == null &&
                rt.ExpiresAt > now)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(rt => rt.RevokedAt, now),
                cancellationToken);

        if (revokedRows != 1)
        {
            return AuthResult<RefreshResponse>.Failure(
                AuthErrorKind.InvalidCredentials,
                "Invalid or expired refresh token.");
        }

        var (newRefreshToken, newRefreshTokenEntity) = CreateRefreshToken(
            refreshToken.UserId,
            now);

        dbContext.Set<RefreshToken>().Add(newRefreshTokenEntity);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return AuthResult<RefreshResponse>.Success(new RefreshResponse
        {
            AccessToken = jwtTokenService.CreateToken(refreshToken.user),
            RefreshToken = newRefreshToken
        });
    }
    public async Task<AuthResult<AuthResponse>> GoogleLoginAsync(
        GoogleAuthRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payload =
                await oAuthProviderService.ValidateGoogleTokenAsync(
                    request.IdToken,
                    cancellationToken);

            var email = payload.Email
                .Trim()
                .ToLowerInvariant();

            var user = await dbContext.Users
                .FirstOrDefaultAsync(
                    u => u.Email == email,
                    cancellationToken);

            if (user is null)
            {
                user = new User
                {
                    Id = Guid.NewGuid(),
                    Email = email,
                    DisplayName = payload.Name ?? email,
                    GoogleId = payload.Subject,
                    CreatedAt = DateTime.UtcNow
                };

                dbContext.Users.Add(user);

                await dbContext.SaveChangesAsync(
                    cancellationToken);
            }
            else if (string.IsNullOrWhiteSpace(user.GoogleId))
            {
                user.GoogleId = payload.Subject;

                await dbContext.SaveChangesAsync(
                    cancellationToken);
            }

            var refreshToken = await CreateRefreshTokenAsync(
                user.Id,
                cancellationToken);

            return AuthResult<AuthResponse>.Success(
                BuildAuthResponse(
                    user,
                    refreshToken));
        }
        catch (Exception)
        {
            return AuthResult<AuthResponse>.Failure(
                AuthErrorKind.InvalidCredentials,
                "Invalid Google token.");
        }
    }

    private async Task<string> CreateRefreshTokenAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var (refreshToken, refreshTokenEntity) = CreateRefreshToken(
            userId,
            DateTime.UtcNow);

        dbContext.Set<RefreshToken>().Add(refreshTokenEntity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return refreshToken;
    }

    private static (string Token, RefreshToken Entity) CreateRefreshToken(
        Guid userId,
        DateTime now)
    {
        var refreshToken = RefreshTokenGenerator.GenerateToken();

        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = RefreshTokenHasher.HashToken(refreshToken),
            CreatedAt = now,
            ExpiresAt = now.AddDays(RefreshTokenLifetimeDays)
        };

        return (refreshToken, refreshTokenEntity);
    }


    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresUniqueViolation
        };

    private AuthResponse BuildAuthResponse(
        User user,
        string refreshToken)
    {
        return new AuthResponse
        {
            UserId = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Token = jwtTokenService.CreateToken(user),
            RefreshToken = refreshToken
        };
    }
}
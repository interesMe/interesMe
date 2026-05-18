using InteresMe.API.Modules.Auth.DTOs;

namespace InteresMe.API.Modules.Auth.Services;

public enum AuthErrorKind
{
    Validation,
    EmailAlreadyExists,
    InvalidCredentials
}

public sealed class AuthResult
{
    public bool IsSuccess { get; init; }

    public AuthResponse? Response { get; init; }

    public AuthErrorKind? ErrorKind { get; init; }

    public string? ErrorMessage { get; init; }

    public static AuthResult Success(AuthResponse response) => new()
    {
        IsSuccess = true,
        Response = response
    };

    public static AuthResult Failure(AuthErrorKind kind, string message) => new()
    {
        IsSuccess = false,
        ErrorKind = kind,
        ErrorMessage = message
    };
}

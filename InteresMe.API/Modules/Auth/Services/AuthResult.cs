using InteresMe.API.Modules.Auth.DTOs;

namespace InteresMe.API.Modules.Auth.Services;

public enum AuthErrorKind
{
    Validation,
    EmailAlreadyExists,
    InvalidCredentials,
    NotImplemented
}

public sealed class AuthResult<T>
{
    public bool IsSuccess { get; init; }

    public T? Response { get; init; }

    public AuthErrorKind? ErrorKind { get; init; }

    public string? ErrorMessage { get; init; }

    public static AuthResult<T> Success(T response) => new()
    {
        IsSuccess = true,
        Response = response
    };

    public static AuthResult<T> Failure(AuthErrorKind kind, string message) => new()
    {
        IsSuccess = false,
        ErrorKind = kind,
        ErrorMessage = message
    };


}

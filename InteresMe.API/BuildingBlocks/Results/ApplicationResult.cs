namespace InteresMe.API.BuildingBlocks.Results;

public sealed class ApplicationResult<T>
{
    public bool IsSuccess { get; init; }

    public T? Response { get; init; }

    public ApplicationErrorKind? ErrorKind { get; init; }

    public string? ErrorMessage { get; init; }

    public static ApplicationResult<T> Success(T response) => new()
    {
        IsSuccess = true,
        Response = response
    };

    public static ApplicationResult<T> Failure(ApplicationErrorKind kind, string message) => new()
    {
        IsSuccess = false,
        ErrorKind = kind,
        ErrorMessage = message
    };
}

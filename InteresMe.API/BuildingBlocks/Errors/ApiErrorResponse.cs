namespace InteresMe.API.BuildingBlocks.Errors;

public sealed class ApiErrorResponse
{
    public string Code { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public int Status { get; init; }

    public string? TraceId { get; init; }

    public Dictionary<string, string[]>? Errors { get; init; }
}

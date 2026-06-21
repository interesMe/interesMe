namespace InteresMe.API.BuildingBlocks.Errors;

public static class ApiErrorCodes
{
    public const string ValidationFailed = "validation.failed";

    public const string Unauthorized = "auth.unauthorized";

    public const string Forbidden = "auth.forbidden";

    public const string ResourceNotFound = "resource.not_found";

    public const string ConflictDetected = "conflict.detected";

    public const string InternalError = "server.internal_error";

    public const string FeatureNotImplemented = "feature.not_implemented";

    public const string RequestTooLarge = "request.too_large";
}

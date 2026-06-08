namespace InteresMe.API.BuildingBlocks.Results;

public enum ApplicationErrorKind
{
    Validation,
    Conflict,
    Unauthorized,
    NotFound,
    Forbidden,
    InternalServerError,
    NotImplemented
}

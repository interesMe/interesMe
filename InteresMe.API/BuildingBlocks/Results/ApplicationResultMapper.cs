using InteresMe.API.BuildingBlocks.Errors;
using Microsoft.AspNetCore.Mvc;

namespace InteresMe.API.BuildingBlocks.Results;

public static class ApplicationResultMapper
{
    public static async Task<IActionResult> ToActionResult<T>(
        Task<ApplicationResult<T>> resultTask) =>
        ToActionResult(await resultTask);

    public static IActionResult ToActionResult<T>(
        ApplicationResult<T> result)
    {
        if (result.IsSuccess)
        {
            return new OkObjectResult(result.Response);
        }

        var status = ToStatusCode(result.ErrorKind);

        return new ApiErrorObjectResult(
            result.ErrorCode ?? ToErrorCode(result.ErrorKind),
            result.ErrorMessage ?? "Request failed.",
            status);
    }

    private static int ToStatusCode(ApplicationErrorKind? kind) =>
        kind switch
        {
            ApplicationErrorKind.Validation => StatusCodes.Status400BadRequest,
            ApplicationErrorKind.Conflict => StatusCodes.Status409Conflict,
            ApplicationErrorKind.Unauthorized => StatusCodes.Status401Unauthorized,
            ApplicationErrorKind.NotFound => StatusCodes.Status404NotFound,
            ApplicationErrorKind.Forbidden => StatusCodes.Status403Forbidden,
            ApplicationErrorKind.InternalServerError => StatusCodes.Status500InternalServerError,
            ApplicationErrorKind.NotImplemented => StatusCodes.Status501NotImplemented,
            _ => StatusCodes.Status400BadRequest
        };

    private static string ToErrorCode(ApplicationErrorKind? kind) =>
        kind switch
        {
            ApplicationErrorKind.Validation => ApiErrorCodes.ValidationFailed,
            ApplicationErrorKind.Conflict => ApiErrorCodes.ConflictDetected,
            ApplicationErrorKind.Unauthorized => ApiErrorCodes.Unauthorized,
            ApplicationErrorKind.NotFound => ApiErrorCodes.ResourceNotFound,
            ApplicationErrorKind.Forbidden => ApiErrorCodes.Forbidden,
            ApplicationErrorKind.InternalServerError => ApiErrorCodes.InternalError,
            ApplicationErrorKind.NotImplemented => ApiErrorCodes.FeatureNotImplemented,
            _ => ApiErrorCodes.ValidationFailed
        };

    private sealed class ApiErrorObjectResult(
        string code,
        string message,
        int status) : ObjectResult(new ApiErrorResponse
        {
            Code = code,
            Message = message,
            Status = status
        })
    {
        public override Task ExecuteResultAsync(ActionContext context)
        {
            Value = new ApiErrorResponse
            {
                Code = code,
                Message = message,
                Status = status,
                TraceId = context.HttpContext.TraceIdentifier
            };

            StatusCode = status;

            return base.ExecuteResultAsync(context);
        }
    }
}

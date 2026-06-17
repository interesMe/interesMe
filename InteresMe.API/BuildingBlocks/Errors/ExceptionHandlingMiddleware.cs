namespace InteresMe.API.BuildingBlocks.Errors;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
            {
                logger.LogError(
                    exception,
                    "Unhandled exception occurred after the response started.");

                throw;
            }

            logger.LogError(exception, "Unhandled exception occurred.");

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(new ApiErrorResponse
            {
                Code = ApiErrorCodes.InternalError,
                Message = "An unexpected error occurred.",
                Status = StatusCodes.Status500InternalServerError,
                TraceId = context.TraceIdentifier
            });
        }
    }
}

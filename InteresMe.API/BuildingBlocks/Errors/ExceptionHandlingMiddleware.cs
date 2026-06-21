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
        catch (BadHttpRequestException exception)
            when (exception.StatusCode == StatusCodes.Status413PayloadTooLarge)
        {
            logger.LogWarning("Request payload exceeded the configured limit.");

            if (!context.Response.HasStarted)
            {
                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                context.Response.ContentType = "application/json";

                await context.Response.WriteAsJsonAsync(new ApiErrorResponse
                {
                    Code = ApiErrorCodes.RequestTooLarge,
                    Message = "Request payload is too large.",
                    Status = StatusCodes.Status413PayloadTooLarge,
                    TraceId = context.TraceIdentifier
                });
            }
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

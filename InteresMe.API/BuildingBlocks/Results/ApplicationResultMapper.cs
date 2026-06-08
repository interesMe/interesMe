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

        return result.ErrorKind switch
        {
            ApplicationErrorKind.Validation =>
                new BadRequestObjectResult(new { message = result.ErrorMessage }),

            ApplicationErrorKind.Conflict =>
                new ConflictObjectResult(new { message = result.ErrorMessage }),

            ApplicationErrorKind.Unauthorized =>
                new UnauthorizedObjectResult(new { message = result.ErrorMessage }),

            ApplicationErrorKind.NotFound =>
                new NotFoundObjectResult(new { message = result.ErrorMessage }),

            ApplicationErrorKind.Forbidden =>
                new ObjectResult(new { message = result.ErrorMessage })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                },

            ApplicationErrorKind.InternalServerError =>
                new ObjectResult(new { message = result.ErrorMessage })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                },

            ApplicationErrorKind.NotImplemented =>
                new ObjectResult(new { message = result.ErrorMessage })
                {
                    StatusCode = StatusCodes.Status501NotImplemented
                },

            _ =>
                new BadRequestObjectResult(new { message = result.ErrorMessage })
        };
    }
}

using InteresMe.API.Modules.Auth.Services;
using InteresMe.API.Modules.Interests.DTOs;
using InteresMe.API.Modules.Interests.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InteresMe.API.Modules.Interests.Controllers;

[ApiController]
[Authorize]
[Route("api/interests")]
public class InterestsController(IInterestService interestService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken) =>
        Ok(await interestService.GetAllAsync(cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateInterestRequest request,
        CancellationToken cancellationToken) =>
        ToActionResult(await interestService.CreateAsync(request, cancellationToken));

    private static IActionResult ToActionResult<T>(
        AuthResult<T> result)
    {
        if (result.IsSuccess)
        {
            return new OkObjectResult(result.Response);
        }

        return result.ErrorKind switch
        {
            AuthErrorKind.Validation =>
                new BadRequestObjectResult(
                    new { message = result.ErrorMessage }),

            AuthErrorKind.EmailAlreadyExists =>
                new ConflictObjectResult(
                    new { message = result.ErrorMessage }),

            AuthErrorKind.InvalidCredentials =>
                new UnauthorizedObjectResult(
                    new { message = result.ErrorMessage }),

            AuthErrorKind.NotImplemented =>
                new ObjectResult(
                    new { message = result.ErrorMessage })
                {
                    StatusCode = StatusCodes.Status501NotImplemented
                },

            _ =>
                new BadRequestObjectResult(
                    new { message = result.ErrorMessage })
        };
    }
}

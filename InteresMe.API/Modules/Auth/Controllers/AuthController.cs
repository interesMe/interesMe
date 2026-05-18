using InteresMe.API.Modules.Auth.DTOs;
using InteresMe.API.Modules.Auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InteresMe.API.Modules.Auth.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken) =>
        ToActionResult(authService.RegisterAsync(request, cancellationToken));

    [HttpPost("login")]
    public Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken) =>
        ToActionResult(authService.LoginAsync(request, cancellationToken));

    private static async Task<IActionResult> ToActionResult(Task<AuthResult> resultTask)
    {
        var result = await resultTask;

        if (result.IsSuccess)
        {
            return new OkObjectResult(result.Response);
        }

        return result.ErrorKind switch
        {
            AuthErrorKind.Validation => new BadRequestObjectResult(new { message = result.ErrorMessage }),
            AuthErrorKind.EmailAlreadyExists => new ConflictObjectResult(new { message = result.ErrorMessage }),
            AuthErrorKind.InvalidCredentials => new UnauthorizedObjectResult(new { message = result.ErrorMessage }),
            _ => new BadRequestObjectResult(new { message = result.ErrorMessage })
        };
    }
}

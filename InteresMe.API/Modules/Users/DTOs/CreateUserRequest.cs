namespace InteresMe.API.Modules.Users.DTOs;

public class CreateUserRequest
{
    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}

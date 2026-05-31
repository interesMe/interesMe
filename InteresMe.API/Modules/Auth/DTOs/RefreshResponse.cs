namespace InteresMe.API.Modules.Auth.DTOs;

public class RefreshResponse
{
    public string AccessToken { get; set; } = string.Empty;

    public string RefreshToken { get; set; } = string.Empty;

}
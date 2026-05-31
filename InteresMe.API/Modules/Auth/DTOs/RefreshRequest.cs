using Microsoft.Win32.SafeHandles;

namespace InteresMe.API.Modules.Auth.DTOs;

public class RefreshRequest
{
    public String RefreshToken { get; set; } = string.Empty;

}
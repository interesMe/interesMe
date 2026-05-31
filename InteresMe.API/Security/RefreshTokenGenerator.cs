using System.Security.Cryptography;
using System.Text;

namespace InteresMe.API.Security;

public static class RefreshTokenGenerator
{
    public static string GenerateToken()
    {
        return Convert.ToBase64String(
            RandomNumberGenerator.GetBytes(64));
    }
}
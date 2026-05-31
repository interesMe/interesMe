using System.Security.Cryptography;
using System.Text;

namespace InteresMe.API.Security;

public static class RefreshTokenHasher
{
    public static string HashToken(string token)
    {
        
        var Hashbytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(Hashbytes);
    }
}
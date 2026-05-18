using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using InteresMe.API.Configuration;
using InteresMe.API.Modules.Auth.Models;
using Microsoft.IdentityModel.Tokens;

namespace InteresMe.API.Security;

public class JwtTokenService
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromDays(7);

    public string CreateToken(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Name, user.DisplayName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(AppSecrets.AuthTokenSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: AppSecrets.JwtIssuer,
            audience: AppSecrets.JwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.Add(TokenLifetime),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

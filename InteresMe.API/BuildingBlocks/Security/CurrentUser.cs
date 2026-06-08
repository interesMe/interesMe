using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace InteresMe.API.BuildingBlocks.Security;

public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid UserId
    {
        get
        {
            var userIdClaim =
                httpContextAccessor.HttpContext?.User.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
                httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (Guid.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }

            throw new InvalidOperationException("Invalid access token.");
        }
    }
}

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
namespace olx_be_api.Helpers
{
    public static class UserClaimsExtension
    {        public static Guid GetUserId(this ClaimsPrincipal user)
        {
            var userId = user?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                      ?? user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? user?.FindFirst("sub")?.Value;
                      
            if (userId != null && Guid.TryParse(userId, out var guid))
            {
                return guid;
            }
            
            return Guid.Empty;
        }
    }
}

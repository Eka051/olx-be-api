using System.Security.Claims;
namespace olx_be_api.Helpers
{
    public static class UserClaimsExtension
    {
        public static Guid GetUserId(this ClaimsPrincipal user)
        {
            var userId = user?.FindFirst("uid")?.Value;
            return userId != null ? Guid.Parse(userId) : Guid.Empty;
        }
    }
}

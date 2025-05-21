using Microsoft.AspNetCore.Mvc;
using olx_be_api.Data;
using olx_be_api.DTO;
using olx_be_api.Helpers;
using olx_be_api.Models;
using Microsoft.EntityFrameworkCore;
using FirebaseAdmin.Auth;

namespace olx_be_api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        public readonly IConfiguration _config;

        public AuthController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _config = configuration;
        }

        [HttpPost("firebase-login")]
        public async Task<IActionResult> FirebaseLogin([FromBody] FirebaseLoginRequest request)
        {
            try
            {
                var decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(request.IdToken);
                var uid = decodedToken.Uid;

                var firebasUser = await FirebaseAuth.DefaultInstance.GetUserAsync(uid);
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.ProviderUid == uid && u.AuthProvider == firebasUser.ProviderId);

                if (user == null)
                {
                    user = new User
                    {
                        Id = Guid.NewGuid(),
                        Name = firebasUser.DisplayName,
                        Email = firebasUser.Email,
                        PhoneNumber = firebasUser.PhoneNumber,
                        ProfilePictureUrl = firebasUser.PhotoUrl,
                        AuthProvider = firebasUser.ProviderId,
                        ProviderUid = uid,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                }
                JwtHelper jwtHelper = new JwtHelper(_config);
                var token = jwtHelper.GenerateJwtToken(user);

            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Firebase login failed", error = ex.Message });
            }
        }

    }
}
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
        //public readonly IConfiguration _config;
        private readonly JwtHelper _jwtHelper;

        public AuthController(AppDbContext context, JwtHelper jwtHelper)
        {
            _context = context;
            _jwtHelper = jwtHelper;
        }

        [HttpPost("firebase-login")]
        public async Task<IActionResult> FirebaseLogin([FromBody] FirebaseLoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Permintaan tidak valid", error = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage });
            }

            try
            {
                FirebaseToken decodedToken;
                try
                {
                    decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(request.IdToken);
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
                    var token = _jwtHelper.GenerateJwtToken(user);
                    return Ok(token);
                }
                catch (Exception ex)
                {
                    return BadRequest(new { success = false, message = "Firebase login failed", error = ex.Message });
                }

            } catch (Exception ex)
            {
                return BadRequest(new { success = false, message = "Firebase login failed", error = ex.Message });
            }

            
        }

        [HttpPost("email-otp")]
        public async Task<IActionResult> LoginWithEmailOtp([FromBody] EmailOtpRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
            {
                user = new User
                {
                    Name = request.Email.Split('@')[0],
                    Email = request.Email,
                    AuthProvider = "email",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            var token = _jwtHelper.GenerateJwtToken(user);
            return Ok(new { token });
        }


    }
}
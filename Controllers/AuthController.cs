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
                }
                catch (FirebaseAuthException ex)
                {
                    return Unauthorized(new { success = false, message = "Token Firebase tidak valid", error = ex.Message });
                }

                var uid = decodedToken.Uid;
                UserRecord firebaseUser;
                try
                {
                    firebaseUser = await FirebaseAuth.DefaultInstance.GetUserAsync(uid);
                }
                catch (FirebaseAuthException ex)
                {
                    return NotFound(new { success = false, message = "Pengguna tidak ditemukan di Firebase", error = ex.Message });
                }

                string authProvider = firebaseUser.ProviderData.FirstOrDefault()?.ProviderId ?? "unknown";
                var user = await _context.Users.FirstOrDefaultAsync(u => u.ProviderUid == uid && u.AuthProvider == authProvider);

                if (user == null)
                {
                    user = new User
                    {
                        Id = Guid.NewGuid(),
                        Name = firebaseUser.DisplayName,
                        Email = firebaseUser.Email,
                        PhoneNumber = firebaseUser.PhoneNumber,
                        ProfilePictureUrl = firebaseUser.PhotoUrl,
                        AuthProvider = authProvider,
                        ProviderUid = uid,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Add(user);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    bool needsUpdate = false;
                    if (user.Name != firebaseUser.DisplayName)
                    {
                        user.Name = firebaseUser.DisplayName;
                        needsUpdate = true;
                    }
                    if (user.Email != firebaseUser.Email)
                    {
                        user.Email = firebaseUser.Email;
                        needsUpdate = true;
                    }
                    if (user.PhoneNumber != firebaseUser.PhoneNumber)
                    {
                        user.PhoneNumber = firebaseUser.PhoneNumber;
                        needsUpdate = true;
                    }
                    if (user.ProfilePictureUrl != firebaseUser.PhotoUrl)
                    {
                        user.ProfilePictureUrl = firebaseUser.PhotoUrl;
                        needsUpdate = true;
                    }

                    if (needsUpdate)
                    {
                        _context.Update(user);
                        await _context.SaveChangesAsync();
                    }
                }

                var token = _jwtHelper.GenerateJwtToken(user);
                return Ok(new LoginResponseDTO 
                {
                    Success = true,
                    Message = "Login Berhasil",
                    Token = token,
                    User = new User
                    {
                        Id = user.Id,
                        Name = user.Name,
                        Email = user.Email,
                        PhoneNumber = user.PhoneNumber,
                        ProfilePictureUrl = user.ProfilePictureUrl,
                        AuthProvider = user.AuthProvider,
                        ProviderUid = user.ProviderUid,
                        CreatedAt = user.CreatedAt
                    }

                });

            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = "Firebase login failed", error = ex.Message });
            }

            
        }

        [HttpPost("send-email-otp")]
        public async Task<IActionResult> LoginWithEmailOtp([FromBody] EmailOtpRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Permintaan tidak valid", error = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email && u.AuthProvider == "email");

            if (user == null)
            {
                user = new User
                {
                    Id = Guid.NewGuid(),
                    Name = request.Email.Split("@")[0],
                    Email = request.Email,
                    AuthProvider = "email",
                    ProviderUid = Guid.NewGuid().ToString(),
                    CreatedAt = DateTime.UtcNow
                };
                _context.Add(user);
                await _context.SaveChangesAsync();
            }
            
            var existingOTPs = _context.EmailOtps.Where(o => o.UserId == user.Id && !o.IsUsed && o.ExpiredAt > DateTime.UtcNow);
            if (existingOTPs.Any())
            {
                _context.EmailOtps.RemoveRange(existingOTPs);
            }

            var otpCode = new Random().Next(100000, 999999).ToString();
            var otpExpiration = DateTime.UtcNow.AddMinutes(10);

            var emailOtp = new EmailOtp
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Email = request.Email,
                Otp = otpCode,
                ExpiredAt = otpExpiration,
                IsUsed = false
            };
            _context.EmailOtps.Add(emailOtp);
            await _context.SaveChangesAsync();

            try
            {
                string emailSubject = "Kode Verifikasi Akun OLX";
                string message = $"Kode OTP Anda adalah: <b>{otpCode}</b><br>Silakan masukkan kode ini untuk melanjutkan proses login Anda. Kode ini berlaku selama 10 menit.";
            } catch (Exception ex)
            {
                return BadRequest(new { success = false, message = "Gagal mengirim email OTP", error = ex.Message });
            }
        }


    }
}
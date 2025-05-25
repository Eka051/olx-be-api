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
        private readonly IEmailHelper _emailHelper;

        public AuthController(AppDbContext context, JwtHelper jwtHelper, IEmailHelper emailHelper)
        {
            _context = context;
            _jwtHelper = jwtHelper;
            _emailHelper = emailHelper;
        }

        [HttpPost("firebase")]
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
                        Name = user.Name,
                        Email = user.Email,
                        PhoneNumber = user.PhoneNumber,
                        ProfilePictureUrl = user.ProfilePictureUrl,
                        AuthProvider = user.AuthProvider,
                        CreatedAt = user.CreatedAt
                    }
                });

            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = "Firebase login failed", error = ex.InnerException?.Message ?? ex.Message });
            }

            
        }

        [HttpPost("email-otps")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<IActionResult> SendEmailOTP([FromBody] EmailOtpRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Permintaan tidak valid", error = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage });
            }

            var recentOtp = await _context.EmailOtps
               .Where(o => o.Email == request.Email && o.CreatedAt > DateTime.UtcNow.AddMinutes(-1))
               .FirstOrDefaultAsync();

            if (recentOtp != null)
            {
                return BadRequest(new { success = false, message = "Anda sudah mengirimkan kode OTP dalam 1 menit terakhir. Silakan tunggu sebelum mencoba lagi." });
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
                string emailMessage = $@"
                <html>
                <body style='font-family: Arial, sans-serif; padding: 20px;'>
                    <h2>Verifikasi Akun OLX</h2>
                    <p>Kode OTP Anda adalah:</p>
                    <h1 style='color: #4285f4; font-size: 32px; letter-spacing: 2px; padding: 10px; background-color: #f1f1f1; display: inline-block; border-radius: 5px;'>{otpCode}</h1>
                    <p>Silakan masukkan kode ini untuk melanjutkan proses login Anda.</p>
                    <p>Kode ini berlaku selama 10 menit.</p>
                    <p>Jika Anda tidak meminta kode ini, silakan abaikan email ini.</p>
                </body>
                </html>";
                await _emailHelper.SendEmailAsync(request.Email, emailSubject, emailMessage);

                return StatusCode(StatusCodes.Status201Created, new { success = true, message = "Kode OTP telah dikirim ke email Anda" });
            } catch (Exception ex)
            {
                _context.EmailOtps.Remove(emailOtp);
                await _context.SaveChangesAsync();
                return BadRequest(new { success = false, message = "Gagal mengirim email OTP", error = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpPost("email-verifications")]
        public async Task<IActionResult> VerifyEmailOtp([FromBody] EmailOtpVerify request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Otp))
                {
                    return BadRequest(new { success = false, message = "Email dan OTP harus diisi" });
                }
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Permintaan tidak valid", error = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage });
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email && u.AuthProvider == "email");
                if (user == null)
                {
                    return NotFound(new { success = false, message = "Pengguna tidak ditemukan" });
                }

                var emailOtp = await _context.EmailOtps.FirstOrDefaultAsync(o => o.UserId == user.Id && o.Otp == request.Otp && !o.IsUsed && o.ExpiredAt > DateTime.UtcNow);
                if (emailOtp == null)
                {
                    return BadRequest(new { success = false, message = "Kode OTP tidak valid atau telah kedaluwarsa" });
                }

                emailOtp.IsUsed = true;
                _context.EmailOtps.Update(emailOtp);
                await _context.SaveChangesAsync();

                var token = _jwtHelper.GenerateJwtToken(user);
                return Ok(new LoginResponseDTO
                {
                    Success = true,
                    Message = "OTP berhasil diverifikasi",
                    Token = token,
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = "Permintaan tidak valid", error = ex.InnerException?.Message ?? ex.Message });
            }
            
        }
    }
}
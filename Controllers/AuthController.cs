using Microsoft.AspNetCore.Authorization;
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
        [ProducesResponseType(typeof(ApiResponse<LoginResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> FirebaseLogin([FromBody] FirebaseLoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiErrorResponse
                {
                    success = false,
                    message = "Permintaan tidak valid",
                    errors = ModelState
                });
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
                    return Unauthorized(new ApiErrorResponse
                    {
                        success = false,
                        message = "Token Firebase tidak valid",
                        errors = new { firebase = ex.Message }
                    });
                }

                var uid = decodedToken.Uid;
                UserRecord firebaseUser;
                try
                {
                    firebaseUser = await FirebaseAuth.DefaultInstance.GetUserAsync(uid);
                }
                catch (FirebaseAuthException ex)
                {
                    return NotFound(new ApiErrorResponse
                    {
                        success = false,
                        message = "Pengguna tidak ditemukan di Firebase",
                        errors = new { firebase = ex.Message }
                    });
                }

                string authProvider = firebaseUser.ProviderData.FirstOrDefault()?.ProviderId ?? "unknown";
                var user = await _context.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.ProviderUid == uid && u.AuthProvider == authProvider);

                if (user == null)
                {
                    var userRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "User");
                    if (userRole == null)
                    {
                        return StatusCode(StatusCodes.Status500InternalServerError, new ApiErrorResponse { message = "Konfigurasi sistem tidak lengkap: Role 'User' tidak ditemukan." });
                    }

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
                    _context.Users.Add(user);
                    _context.UserRoles.Add(new UserRole { User = user, Role = userRole });
                    await _context.SaveChangesAsync();
                }
                else
                {
                    bool needsUpdate = false;

                    if (string.IsNullOrEmpty(user.Name) && !string.IsNullOrEmpty(firebaseUser.DisplayName))
                    {
                        user.Name = firebaseUser.DisplayName;
                        needsUpdate = true;
                    }

                    if (string.IsNullOrEmpty(user.PhoneNumber) && !string.IsNullOrEmpty(firebaseUser.PhoneNumber))
                    {
                        user.PhoneNumber = firebaseUser.PhoneNumber;
                        needsUpdate = true;
                    }

                    if (string.IsNullOrEmpty(user.ProfilePictureUrl) && !string.IsNullOrEmpty(firebaseUser.PhotoUrl))
                    {
                        user.ProfilePictureUrl = firebaseUser.PhotoUrl;
                        needsUpdate = true;
                    }

                    if (user.Email != firebaseUser.Email)
                    {
                        user.Email = firebaseUser.Email;
                        needsUpdate = true;
                    }

                    if (needsUpdate)
                    {
                        _context.Update(user);
                        await _context.SaveChangesAsync();
                    }
                }

                var token = _jwtHelper.GenerateJwtToken(user);
                var loginResponse = new LoginResponseDTO
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
                };

                return Ok(new ApiResponse<LoginResponseDTO>
                {
                    success = true,
                    message = "Login berhasil",
                    data = loginResponse
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ApiErrorResponse
                    {
                        success = false,
                        message = "Terjadi kesalahan internal server",
                        errors = new { error = ex.Message }
                    });
            }
        }

        [HttpPost("email/otp")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SendEmailOTP([FromBody] EmailOtpRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiErrorResponse { success = false, message = "Permintaan tidak valid", errors = ModelState });
            }

            var recentOtp = await _context.EmailOtps
               .Where(o => o.Email == request.Email && o.CreatedAt > DateTime.UtcNow.AddMinutes(-1))
               .FirstOrDefaultAsync();

            if (recentOtp != null)
            {
                return StatusCode(StatusCodes.Status429TooManyRequests, new ApiErrorResponse { success = false, message = "Anda baru saja meminta kode OTP. Silakan tunggu 1 menit sebelum mencoba lagi." });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user != null)
            {
                if (user.AuthProvider != "email")
                {
                    return BadRequest(new ApiErrorResponse { success = false, message = "Email ini terdaftar dengan metode login lain (misal: Google). Silakan login menggunakan metode tersebut." });
                }
            }
            else
            {
                var userRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "User");
                if (userRole == null)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, new ApiErrorResponse { message = "Konfigurasi sistem tidak lengkap: Role 'User' tidak ditemukan." });
                }

                user = new User
                {
                    Id = Guid.NewGuid(),
                    Name = request.Email.Split("@")[0],
                    Email = request.Email,
                    AuthProvider = "email",
                    ProviderUid = Guid.NewGuid().ToString(),
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                _context.UserRoles.Add(new UserRole { User = user, Role = userRole });
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
                UserId = user.Id,
                Email = request.Email,
                Otp = otpCode,
                CreatedAt = DateTime.UtcNow,
                ExpiredAt = otpExpiration,
                IsUsed = false
            };
            _context.EmailOtps.Add(emailOtp);

            try
            {
                await _context.SaveChangesAsync();

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

                return Created("", new ApiResponse<string>
                {
                    success = true,
                    message = "Kode OTP telah dikirim ke email Anda"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ApiErrorResponse
                    {
                        success = false,
                        message = "Gagal memproses permintaan OTP.",
                        errors = new { error = ex.Message }
                    });
            }
        }

        [HttpPost("email/verify")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status410Gone)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> VerifyEmailOtp([FromBody] EmailOtpVerify request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Otp))
                {
                    return BadRequest(new ApiErrorResponse
                    {
                        success = false,
                        message = "Email dan OTP harus diisi"
                    });
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiErrorResponse
                    {
                        success = false,
                        message = "Permintaan tidak valid",
                        errors = ModelState
                    });
                }

                var user = await _context.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Email == request.Email && u.AuthProvider == "email");

                if (user == null)
                {
                    return NotFound(new ApiErrorResponse
                    {
                        success = false,
                        message = "Pengguna tidak ditemukan"
                    });
                }

                var emailOtp = await _context.EmailOtps.FirstOrDefaultAsync(o => o.UserId == user.Id && o.Otp == request.Otp && !o.IsUsed && o.ExpiredAt > DateTime.UtcNow);
                if (emailOtp == null)
                {
                    var expiredOtp = await _context.EmailOtps.FirstOrDefaultAsync(o => o.UserId == user.Id && o.Otp == request.Otp);
                    if (expiredOtp != null && expiredOtp.ExpiredAt <= DateTime.UtcNow)
                    {
                        return StatusCode(StatusCodes.Status410Gone,
                            new ApiErrorResponse
                            {
                                success = false,
                                message = "Kode OTP telah kedaluwarsa"
                            });
                    }

                    return BadRequest(new ApiErrorResponse
                    {
                        success = false,
                        message = "Kode OTP tidak valid"
                    });
                }

                emailOtp.IsUsed = true;
                _context.EmailOtps.Update(emailOtp);
                await _context.SaveChangesAsync();

                var token = _jwtHelper.GenerateJwtToken(user);

                return Ok(new ApiResponse<object>
                {
                    success = true,
                    message = "OTP berhasil diverifikasi",
                    data = new { token = token }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ApiErrorResponse
                    {
                        success = false,
                        message = "Terjadi kesalahan internal server",
                        errors = new { error = ex.Message }
                    });
            }
        }

        [HttpGet("test-token")]
        [Authorize]
        public async Task<IActionResult> TestToken()
        {
            var userId = User.GetUserId();
            var user = await _context.Users.FindAsync(userId);

            return Ok(new ApiResponse<object>
            {
                success = true,
                message = "Token test successful",
                data = new
                {
                    userId = userId,
                    userExists = user != null,
                    claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList(),
                    userEmail = user?.Email,
                    userName = user?.Name
                }
            });
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using olx_be_api.Data;
using olx_be_api.DTO;
using olx_be_api.Helpers;
using olx_be_api.Models;
using olx_be_api.Services;
using System;
using System.Threading.Tasks;

namespace olx_be_api.Controllers
{
    [Route("api/users")]
    [ApiController]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IStorageService _storageService;

        public ProfileController(AppDbContext context, IStorageService storageService)
        {
            _context = context;
            _storageService = storageService;
        }

        [HttpGet("me")]
        [ProducesResponseType(typeof(ApiResponse<UserProfileDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = User.GetUserId();
            var user = await _context.Users
                .Include(u => u.Products)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound(new ApiErrorResponse { success = false, message = "Pengguna tidak ditemukan" });
            }

            var userProfileDto = new UserProfileDTO
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                ProfilePictureUrl = user.ProfilePictureUrl,
                CreatedAt = user.CreatedAt,
                TotalAds = user.Products.Count
            };

            return Ok(new ApiResponse<UserProfileDTO> { success = true, message = "Profile retrieved", data = userProfileDto });
        }

        [HttpPut("me")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateMyProfile([FromForm] UpdateProfileDTO profileDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiErrorResponse { success = false, message = "Invalid data", errors = ModelState });
            }

            var userId = User.GetUserId();
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound(new ApiErrorResponse { success = false, message = "Pengguna tidak ditemukan" });
            }

            bool isUpdated = false;

            if (!string.IsNullOrEmpty(profileDto.Name) && user.Name != profileDto.Name)
            {
                user.Name = profileDto.Name;
                isUpdated = true;
            }

            if (!string.IsNullOrEmpty(profileDto.PhoneNumber) && user.PhoneNumber != profileDto.PhoneNumber)
            {
                var phoneExists = await _context.Users.AnyAsync(u => u.PhoneNumber == profileDto.PhoneNumber && u.Id != userId);
                if (phoneExists)
                {
                    return BadRequest(new ApiErrorResponse { success = false, message = "Nomor telepon sudah digunakan." });
                }
                user.PhoneNumber = profileDto.PhoneNumber;
                isUpdated = true;
            }

            if (!string.IsNullOrEmpty(profileDto.Email) && user.Email != profileDto.Email)
            {
                var emailExists = await _context.Users.AnyAsync(u => u.Email == profileDto.Email && u.Id != userId);
                if (emailExists)
                {
                    return BadRequest(new ApiErrorResponse { success = false, message = "Email sudah digunakan." });
                }
                user.Email = profileDto.Email;
                isUpdated = true;
            }

            if (profileDto.ProfilePicture != null && profileDto.ProfilePicture.Length > 0)
            {
                if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
                {
                    try
                    {
                        await _storageService.DeleteAsync(user.ProfilePictureUrl, "user-avatars");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Gagal menghapus foto profil lama {user.ProfilePictureUrl}: {ex.Message}");
                    }
                }

                try
                {
                    var newProfilePictureUrl = await _storageService.UploadAsync(profileDto.ProfilePicture, "user-avatars");
                    user.ProfilePictureUrl = newProfilePictureUrl;
                    isUpdated = true;
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new ApiErrorResponse { message = $"Gagal upload foto profil: {ex.Message}" });
                }
            }

            if (isUpdated)
            {
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                return Ok(new ApiResponse<string> { success = true, message = "Profil berhasil diperbarui" });
            }

            return Ok(new ApiResponse<string> { success = true, message = "Tidak ada perubahan pada profil" });
        }
    }
}
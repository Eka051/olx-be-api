using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using olx_be_api.Data;
using olx_be_api.DTO;
using olx_be_api.Helpers;
using olx_be_api.Models;
using System.Security.Claims;

namespace olx_be_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly JwtHelper _authHelper;

        public UsersController(AppDbContext context, JwtHelper authHelper)
        {
            _context = context;
            _authHelper = authHelper;
        }

        [HttpGet("profile")]
        public async Task<ActionResult<UserResponseDto>> GetCurrentUser()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized();
            }

            var user = await _context.Users.FindAsync(userGuid);

            if (user == null)
            {
                return NotFound();
            }

            return new UserResponseDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                ProfileImageUrl = user.ProfileImageUrl,
                CreatedAt = user.CreatedAt
            };
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserDto updateUserDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized();
            }

            var user = await _context.Users.FindAsync(userGuid);

            if (user == null)
            {
                return NotFound();
            }

            if (updateUserDto.Name != null)
                user.Name = updateUserDto.Name;
                
            if (updateUserDto.PhoneNumber != null)
                user.PhoneNumber = updateUserDto.PhoneNumber;
                
            if (updateUserDto.ProfileImageUrl != null)
                user.ProfileImageUrl = updateUserDto.ProfileImageUrl;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserResponseDto>> GetUser(Guid id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            return new UserResponseDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                ProfileImageUrl = user.ProfileImageUrl,
                CreatedAt = user.CreatedAt
            };
        }
    }
}
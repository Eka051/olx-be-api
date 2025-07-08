using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using olx_be_api.Data;
using olx_be_api.DTO;
using olx_be_api.Helpers;
using olx_be_api.Models;
using System.Security.Claims;

namespace olx_be_api.Controllers
{
    [Route("api/")]
    [ApiController]
    public class PremiumController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PremiumController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("premium-packages")]
        [ProducesResponseType(typeof(ApiResponse<List<PremiumPackageResponseDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPremiumPackages()
        {
            var packages = await _context.PremiumPackages.ToListAsync();

            if (!packages.Any())
            {
                return NotFound(new ApiErrorResponse
                {
                    success = false,
                    message = "Tidak ada paket premium yang tersedia."
                });
            }

            var response = packages.Select(p => new PremiumPackageResponseDTO
            {
                Id = p.Id,
                Description = p.Description,
                Price = p.Price,
                DurationDays = p.DurationDays,
                IsActive = p.IsActive
            }).ToList();

            return Ok(new ApiResponse<List<PremiumPackageResponseDTO>>
            {
                success = true,
                message = "Berhasil mengambil data paket premium.",
                data = response
            });
        }

        [HttpPost("premium-packages")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<PremiumPackageResponseDTO>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreatePremiumPackage([FromBody] PremiumPackageCreateDTO createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiErrorResponse
                {
                    success = false,
                    message = "Data tidak valid.",
                    errors = ModelState
                });
            }

            var existingPackage = await _context.PremiumPackages
                .FirstOrDefaultAsync(p => p.DurationDays == createDto.DurationDays && p.Price == createDto.Price);
            if (existingPackage != null)
            {
                return Conflict(new ApiErrorResponse
                {
                    success = false,
                    message = "Paket premium dengan nama tersebut sudah ada."
                });
            }

            var package = new PremiumPackage
            {
                Description = createDto.Description!,
                Price = createDto.Price,
                DurationDays = createDto.DurationDays,
                IsActive = createDto.IsActive
            };

            _context.PremiumPackages.Add(package);
            await _context.SaveChangesAsync();

            var response = new PremiumPackageResponseDTO
            {
                Id = package.Id,
                Description = package.Description,
                Price = package.Price,
                DurationDays = package.DurationDays,
                IsActive = package.IsActive
            };

            return CreatedAtAction(nameof(GetPremiumPackages), new { id = package.Id }, new ApiResponse<PremiumPackageResponseDTO>
            {
                success = true,
                message = "Berhasil membuat paket premium.",
                data = response
            });
        }

        [HttpPost("purchase-premium")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<UserPremiumStatusDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> PurchasePremium([FromBody] PremiumPurchaseDTO purchaseDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return BadRequest(new ApiErrorResponse
                {
                    success = false,
                    message = "ID pengguna tidak valid."
                });
            }

            var user = await _context.Users.FindAsync(userGuid);
            if (user == null)
            {
                return NotFound(new ApiErrorResponse
                {
                    success = false,
                    message = "Pengguna tidak ditemukan."
                });
            }

            var package = await _context.PremiumPackages.FindAsync(purchaseDto.PackageId);
            if (package == null)
            {
                return NotFound(new ApiErrorResponse
                {
                    success = false,
                    message = "Paket premium tidak ditemukan."
                });
            }

            DateTime newPremiumUntil;
            if (user.PremiumUntil != null && user.PremiumUntil > DateTime.UtcNow)
            {
                newPremiumUntil = user.PremiumUntil.Value.AddDays(package.DurationDays);
            }
            else
            {
                newPremiumUntil = DateTime.UtcNow.AddDays(package.DurationDays);
            }

            user.ProfileType = ProfileType.Premium;
            user.PremiumUntil = newPremiumUntil;

            var transaction = new Transaction
            {
                User = user,
                Amount = package.Price,
                Details = $"Premium package purchase: {package.Description}",
                CreatedAt = DateTime.UtcNow
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            var response = new UserPremiumStatusDTO
            {
                IsPremium = true,
                ProfileType = ProfileType.Premium,
                PremiumUntil = newPremiumUntil
            };

            return Ok(new ApiResponse<UserPremiumStatusDTO>
            {
                success = true,
                message = "Berhasil membeli paket premium.",
                data = response
            });
        }

        [HttpPut("premium-packages/{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<PremiumPackageResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdatePremiumPackage(int id, [FromBody] PremiumPackageCreateDTO updateDto)
        {
            var package = await _context.PremiumPackages.FindAsync(id);
            if (package == null)
            {
                return NotFound(new ApiErrorResponse
                {
                    success = false,
                    message = "Paket premium tidak ditemukan."
                });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiErrorResponse
                {
                    success = false,
                    message = "Data tidak valid.",
                    errors = ModelState
                });
            }

            var existingPackage = await _context.PremiumPackages
                .FirstOrDefaultAsync(p => p.DurationDays == updateDto.DurationDays && p.Price == updateDto.Price);
            if (existingPackage != null)
            {
                return Conflict(new ApiErrorResponse
                {
                    success = false,
                    message = "Paket premium dengan nama tersebut sudah ada."
                });
            }

            package.Description = updateDto.Description!;
            package.Price = updateDto.Price;
            package.DurationDays = updateDto.DurationDays;
            package.IsActive = updateDto.IsActive;

            _context.PremiumPackages.Update(package);
            await _context.SaveChangesAsync();

            var response = new PremiumPackageResponseDTO
            {
                Id = package.Id,
                Description = package.Description,
                Price = package.Price,
                DurationDays = package.DurationDays,
                IsActive = package.IsActive
            };

            return Ok(new ApiResponse<PremiumPackageResponseDTO>
            {
                success = true,
                message = "Berhasil memperbarui paket premium.",
                data = response
            });
        }

        [HttpDelete("premium-packages/{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeletePremiumPackage(int id)
        {
            var package = await _context.PremiumPackages.FindAsync(id);
            if (package == null)
            {
                return NotFound(new ApiErrorResponse
                {
                    success = false,
                    message = "Paket premium tidak ditemukan."
                });
            }

            _context.PremiumPackages.Remove(package);
            await _context.SaveChangesAsync();

            return NoContent();
        }

    }
}

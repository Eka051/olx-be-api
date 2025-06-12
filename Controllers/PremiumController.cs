using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using olx_be_api.Data;
using olx_be_api.DTO;
using olx_be_api.Helpers;
using olx_be_api.Models;

namespace olx_be_api.Controllers
{
    [Route("api/premium")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class PremiumController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PremiumController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("packages")]
        [ProducesResponseType(typeof(ApiResponse<List<PremiumPackageResponseDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
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
                Name = p.Name,
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

        [HttpPost("packages")]
        [ProducesResponseType(typeof(ApiResponse<PremiumPackageResponseDTO>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
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

            var package = new PremiumPackage
            {
                Name = createDto.Name,
                Price = createDto.Price,
                DurationDays = createDto.DurationDays,
                IsActive = createDto.IsActive
            };

            _context.PremiumPackages.Add(package);
            await _context.SaveChangesAsync();

            var response = new PremiumPackageResponseDTO
            {
                Id = package.Id,
                Name = package.Name,
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

        [HttpPut("packages/{id}")]
        [ProducesResponseType(typeof(ApiResponse<PremiumPackageResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
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

            package.Name = updateDto.Name;
            package.Price = updateDto.Price;
            package.DurationDays = updateDto.DurationDays;
            package.IsActive = updateDto.IsActive;

            _context.PremiumPackages.Update(package);
            await _context.SaveChangesAsync();

            var response = new PremiumPackageResponseDTO
            {
                Id = package.Id,
                Name = package.Name,
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

        [HttpDelete("packages/{id}")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
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

            return Ok(new ApiResponse<string>
            {
                success = true,
                message = "Berhasil menghapus paket premium.",
                data = $"Paket dengan ID {id} telah dihapus."
            });
        }
    }
}

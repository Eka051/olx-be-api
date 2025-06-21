using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using olx_be_api.Data;
using olx_be_api.DTO;
using olx_be_api.Helpers;
using olx_be_api.Models;
using olx_be_api.Models.Enums;
using System.Collections.Generic;
using System.Linq;

namespace olx_be_api.Controllers
{
    [Route("api/adPackage")]
    [ApiController]
    public class AdPackageController : ControllerBase
    {
        private readonly AppDbContext _context;
        
        public AdPackageController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<List<AdPackageDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllAdPackages()
        {
            var adPackages = await _context.AdPackages.Include(ap => ap.Features).ToListAsync();
            var response = adPackages.Select(ap => new AdPackageDTO
            {
                Id = ap.Id,
                Name = ap.Name,
                Price = ap.Price,
                Features = ap.Features.Select(f => new AdPackageFeatureDTO
                {
                    FeatureType = f.FeatureType,
                    Quantity = f.Quantity,
                    DurationDays = f.DurationDays,
                }).ToList()
            }).ToList();
            if (response.Count == 0)
            {                return NotFound(new ApiErrorResponse
                {
                    success = false,
                    message = "Paket iklan tidak ditemukan"
                });
            }
            
            return Ok(new ApiResponse<List<AdPackageDTO>>
            {
                success = true,
                message = "Berhasil mengambil data paket iklan",
                data = response
            });
        }

        [HttpGet("{id}")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<AdPackageDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAdPackageById(int id)
        {
            var adPackage = await _context.AdPackages.Include(ap => ap.Features).FirstOrDefaultAsync(ap => ap.Id == id);
            if (adPackage == null)
            {
                return NotFound(new ApiErrorResponse
                {
                    success = false,
                    message = $"Paket iklan dengan id {id} tidak ditemukan"
                });
            }
            var response = new AdPackageDTO
            {
                Id = adPackage.Id,
                Name = adPackage.Name,
                Price = adPackage.Price,
                Features = adPackage.Features.Select(f => new AdPackageFeatureDTO
                {
                    FeatureType = f.FeatureType,
                    Quantity = f.Quantity,
                    DurationDays = f.DurationDays,
                }).ToList()
            };
            return Ok(new ApiResponse<AdPackageDTO>
            {
                success = true,
                message = "Berhasil mengambil data paket iklan",
                data = response
            });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<AdPackageDTO>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateAdPackage([FromBody] CreateAdPackageDTO createAdPackageDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiErrorResponse
                {
                    success = false,
                    message = "Data inputan tidak valid",
                    errors = ModelState
                });
            }

            var existingPackage = await _context.AdPackages
                .FirstOrDefaultAsync(ap => ap.Name.ToLower() == createAdPackageDto.Name.ToLower());
            if (existingPackage != null)
            {
                return Conflict(new ApiErrorResponse
                {
                    success = false,
                    message = "Nama paket iklan sudah ada"
                });
            }

            var adPackage = new AdPackage
            {
                Name = createAdPackageDto.Name,
                Price = createAdPackageDto.Price,
                Features = createAdPackageDto.Features?.Select(f => new AdPackageFeature
                {
                    FeatureType = f.FeatureType,
                    Quantity = f.Quantity,
                    DurationDays = f.DurationDays
                }).ToList() ?? new List<AdPackageFeature>()
            };

            _context.AdPackages.Add(adPackage);
            await _context.SaveChangesAsync();

            var response = new AdPackageDTO
            {
                Id = adPackage.Id,
                Name = adPackage.Name,
                Price = adPackage.Price,
                Features = adPackage.Features.Select(f => new AdPackageFeatureDTO                {
                    FeatureType = f.FeatureType,
                    Quantity = f.Quantity,
                    DurationDays = f.DurationDays,
                }).ToList()
            };
            
            return CreatedAtAction(nameof(GetAdPackageById), new { id = adPackage.Id }, new ApiResponse<AdPackageDTO>
            {
                success = true,
                message = "Berhasil menambahkan paket iklan",
                data = response
            });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<AdPackageDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateAdPackage(int id, [FromBody] UpdateAdPackageDTO updateAdPackageDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiErrorResponse
                {
                    success = false,
                    message = "Data input tidak valid",
                    errors = ModelState
                });
            }
            
            if (id <= 0)
            {                return BadRequest(new ApiErrorResponse
                {
                    success = false,
                    message = "ID paket iklan tidak valid"
                });
            }
            
            if (updateAdPackageDto == null)
            {
                return BadRequest(new ApiErrorResponse
                {
                    success = false,
                    message = "Data paket iklan tidak boleh kosong"
                });
            }

            var adPackage = await _context.AdPackages.Include(ap => ap.Features).FirstOrDefaultAsync(ap => ap.Id == id);
            if (adPackage == null)
            {
                return NotFound(new ApiErrorResponse
                {
                    success = false,
                    message = "Data paket iklan tidak ditemukan"
                });
            }
            
            var existingPackage = await _context.AdPackages.FirstOrDefaultAsync(ap => ap.Name.ToLower() == updateAdPackageDto.Name.ToLower() && ap.Id != id);
            if (existingPackage != null)
            {
                return Conflict(new ApiErrorResponse
                {
                    success = false,
                    message = "Nama paket iklan sudah ada"
                });
            }
              adPackage.Name = updateAdPackageDto.Name;
            adPackage.Price = updateAdPackageDto.Price;
            
            if (updateAdPackageDto.Features != null && updateAdPackageDto.Features.Any())
            {
                _context.AdPackageFeatures.RemoveRange(adPackage.Features);
                adPackage.Features = updateAdPackageDto.Features.Select(f => new AdPackageFeature
                {
                    FeatureType = f.FeatureType,
                    Quantity = f.Quantity,
                    DurationDays = f.DurationDays,
                    AdPackageId = adPackage.Id
                }).ToList();
            }
              _context.AdPackages.Update(adPackage);
            await _context.SaveChangesAsync();
            
            var response = new AdPackageDTO
            {
                Id = adPackage.Id,
                Name = adPackage.Name,
                Price = adPackage.Price,
                Features = adPackage.Features.Select(f => new AdPackageFeatureDTO
                {
                    FeatureType = f.FeatureType,
                    Quantity = f.Quantity,
                    DurationDays = f.DurationDays,
                }).ToList()
            };
            
            return Ok(new ApiResponse<AdPackageDTO>
            {
                success = true,
                message = "Berhasil mengubah data paket iklan",
                data = response
            });
        }

        [HttpPatch("price/{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<AdPackageDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdatePrice(int id, [FromBody] UpdatePriceAdPackageDTO updatePrice)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiErrorResponse
                {
                    success = false,
                    message = "Data input tidak valid",
                    errors = ModelState
                });
            }
            
            var adPackage = await _context.AdPackages.Include(ap => ap.Features).FirstOrDefaultAsync(ap => ap.Id == id);
            if (adPackage == null)
            {
                return NotFound(new ApiErrorResponse
                {
                    success = false,
                    message = "Data paket iklan tidak ditemukan"
                });
            }
            
            adPackage.Price = updatePrice.Price;
            _context.AdPackages.Update(adPackage);            await _context.SaveChangesAsync();
            
            var response = new AdPackageDTO
            {
                Id = adPackage.Id,
                Name = adPackage.Name,
                Price = adPackage.Price,
                Features = adPackage.Features.Select(f => new AdPackageFeatureDTO
                {
                    FeatureType = f.FeatureType,
                    Quantity = f.Quantity,
                    DurationDays = f.DurationDays,
                }).ToList()
            };
            
            return Ok(new ApiResponse<AdPackageDTO>
            {
                success = true,
                message = "Berhasil mengubah harga paket iklan",
                data = response
            });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteAdPackage(int id)
        {
            var adPackage = await _context.AdPackages.Include(ap => ap.Features).FirstOrDefaultAsync(ap => ap.Id == id);
            if (adPackage == null)
            {
                return NotFound(new ApiErrorResponse
                {
                    success = false,
                    message = "Data paket iklan tidak ditemukan"
                });
            }
            
            _context.AdPackages.Remove(adPackage);
            await _context.SaveChangesAsync();
            
            return Ok(new ApiResponse<string>
            {
                success = true,
                message = "Berhasil menghapus paket iklan",
                data = $"Paket iklan dengan ID {id} telah dihapus."
            });
        }

        [HttpPost("{productId}/purchase")]
        [Authorize]
        public async Task<IActionResult> PurchaseAdPackage(int productId, [FromBody] PurchaseAdPackageDTO purchaseDto)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null)
                return NotFound(new ApiErrorResponse { success = false, message = "Product not found" });

            var adPackage = await _context.AdPackages.FirstOrDefaultAsync(ap => ap.Id == purchaseDto.AdPackageId);
            if (adPackage == null)
                return NotFound(new ApiErrorResponse { success = false, message = "Ad package not found" });

            product.ExpiredAt = DateTime.UtcNow.AddDays(adPackage.DurationDays);
            product.IsActive = true;

            _context.Products.Update(product);
            await _context.SaveChangesAsync();

            return Ok(new ApiResponse<string> { success = true, message = "Ad package purchased successfully." });
        }
    }
}

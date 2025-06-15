using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using olx_be_api.Data;
using olx_be_api.DTO;
using olx_be_api.Helpers;
using olx_be_api.Models;

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
        public IActionResult GetAllAdPackages()
        {
            var adPackages = _context.AdPackages.ToList();
            var response = adPackages.Select(ap => new AdPackageDTO
            {
                Id = ap.Id,
                Name = ap.Name,
                Price = ap.Price,
                DurationDays = ap.DurationDays,
            }).ToList();
            if (response.Count == 0)
            {
                return NotFound(new ApiErrorResponse
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
        public IActionResult GetAdPackageById(int id)
        {
            var adPackage = _context.AdPackages.Find(id);
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
                DurationDays = adPackage.DurationDays
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
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public IActionResult CreateAdPackage([FromBody] CreateAdPackageDTO createAdPackageDto)
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
            var adPackage = new AdPackage
            {
                Name = createAdPackageDto.Name,
                Type = createAdPackageDto.Type,
                Price = createAdPackageDto.Price,
                DurationDays = createAdPackageDto.DurationDays
            };
            _context.AdPackages.Add(adPackage);
            _context.SaveChanges();
            var response = new AdPackageDTO
            {
                Id = adPackage.Id,
                Name = adPackage.Name,
                Price = adPackage.Price,
                DurationDays = adPackage.DurationDays
            };
            return CreatedAtAction(nameof(GetAllAdPackages), new { id = adPackage.Id }, new ApiResponse<AdPackageDTO>
            {
                success = true,
                message = "Berhasil menambahkan iklan",
                data = response
            });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<AdPackageDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public IActionResult UpdateAdPackage(int id, [FromBody] UpdateAdPackageDTO updateAdPackageDto)
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
            var adPackage = _context.AdPackages.Find(id);
            if (adPackage == null)
            {
                return NotFound(new ApiErrorResponse
                {
                    success = false,
                    message = "Data paket iklan tidak ditemukan"
                });
            }

            var existingPackage = _context.AdPackages.FirstOrDefault(ap => ap.Name == updateAdPackageDto.Name && ap.Id != id);
            if (existingPackage != null)
            {
                return BadRequest(new ApiErrorResponse
                {
                    success = false,
                    message = "Nama paket iklan sudah ada"
                });
            }
            adPackage.Name = updateAdPackageDto.Name;
            adPackage.Type = updateAdPackageDto.Type;
            adPackage.Price = updateAdPackageDto.Price;
            adPackage.DurationDays = updateAdPackageDto.DurationDays;
            _context.AdPackages.Update(adPackage);
            _context.SaveChanges();
            var response = new AdPackageDTO
            {
                Id = adPackage.Id,
                Name = adPackage.Name,
                Price = adPackage.Price,
                DurationDays = adPackage.DurationDays
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
        public IActionResult UpdatePrice(int id, [FromBody] UpdatePriceAdPackageDTO updatePrice)
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
            var adPackage = _context.AdPackages.Find(id);
            if (adPackage == null)
            {
                return NotFound(new ApiErrorResponse
                {
                    success = false,
                    message = "Data paket iklan tidak ditemukan"
                });
            }
            adPackage.Price = updatePrice.Price;
            _context.AdPackages.Update(adPackage);
            _context.SaveChanges();
            var response = new AdPackageDTO
            {
                Id = adPackage.Id,
                Name = adPackage.Name,
                Price = adPackage.Price,
                DurationDays = adPackage.DurationDays
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
        public IActionResult DeleteAdPackage(int id)
        {
            var adPackage = _context.AdPackages.Find(id);
            if (adPackage == null)
            {
                return NotFound(new ApiErrorResponse
                {
                    success = false,
                    message = "Data paket iklan tidak ditemukan"
                });
            }
            _context.AdPackages.Remove(adPackage);
            _context.SaveChanges();
            return Ok(new ApiResponse<string>
            {
                success = true,
                message = "Berhasil menghapus paket iklan",
                data = $"Paket iklan dengan ID {id} telah dihapus."
            });
        }
    }
}

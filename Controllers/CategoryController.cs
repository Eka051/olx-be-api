using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using olx_be_api.Data;
using olx_be_api.DTO;
using olx_be_api.Helpers;
using olx_be_api.Models;

namespace olx_be_api.Controllers
{
    [Route("api/categories")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CategoryController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Authorize(Roles="Admin")]
        [ProducesResponseType(typeof(ApiResponse<List<CategoryResponseDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public IActionResult GetAllCategories()
        {
            var categories = _context.Categories.ToList();
            var response = categories.Select(c => new CategoryResponseDto
            {
                Id = c.Id,
                Name = c.Name,

            }).ToList();
            if (response.Count == 0)
            {
                return NotFound(new ApiErrorResponse
                {
                    success = false,
                    message = "Data kategori tidak ditemukan"
                });
            }
            return Ok(new ApiResponse<List<CategoryResponseDto>>
            {
                success = true,
                message = "Berhasil mengambil data kategori",
                data = response
            });

        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<CategoryResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse),StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public IActionResult CreateCategory([FromBody] CreateCategoryDto createCategoryDto)
        {
            try
            {
                if (createCategoryDto == null || string.IsNullOrEmpty(createCategoryDto.Name))
                {
                    return BadRequest(new ApiErrorResponse
                    {
                        success = false,
                        message = "Nama kategori tidak boleh kosong",
                    });
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    return BadRequest(new ApiErrorResponse
                    {
                        success = false,
                        message = "Data kategori tidak valid",
                        errors = errors
                    });
                }

                var existingCategory = _context.Categories.FirstOrDefault(c => c.Name == createCategoryDto.Name);
                if (existingCategory != null)
                {
                    return Conflict(new ApiErrorResponse
                    {
                        success = false,
                        message = $"Kategori {existingCategory.Name} sudah ada"
                    });
                }

                var newCategory = new Category
                {
                    Name = createCategoryDto.Name
                };

                _context.Categories.Add(newCategory);
                _context.SaveChanges();

                var response = new ApiResponse<CategoryResponseDto>
                {
                    success = true,
                    message = "Berhasil membuat kategori baru",
                    data = new CategoryResponseDto
                    {
                        Id = newCategory.Id,
                        Name = newCategory.Name
                    }
                };

                return CreatedAtAction(nameof(GetAllCategories), new { id = newCategory.Id }, response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiErrorResponse
                {
                    success = false,
                    message = "Data invalid",
                    errors = ex.Message
                });
            }
        }
        [HttpPost("batch")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public IActionResult CreateCategories([FromBody] List<CreateCategoryDto> createCategoryDtos)
        {
            if (createCategoryDtos == null || !createCategoryDtos.Any())
            {
                return BadRequest(new ApiErrorResponse
                {
                    success = false,
                    message = "Request body tidak boleh kosong."
                });
            }

            var createdCategories = new List<CategoryResponseDto>();
            var skippedCategories = new List<object>();
            var categoriesToAdd = new List<Category>();

            var existingCategoryNames = _context.Categories
                                                .Select(c => c.Name.ToUpper())
                                                .ToHashSet();

            foreach (var dto in createCategoryDtos)
            {
                if (string.IsNullOrWhiteSpace(dto.Name))
                {
                    skippedCategories.Add(new { name = dto.Name, reason = "Nama kategori tidak boleh kosong." });
                    continue;
                }

                var normalizedName = dto.Name.ToUpper();
                if (existingCategoryNames.Contains(normalizedName) || categoriesToAdd.Any(c => c.Name.ToUpper() == normalizedName))
                {
                    skippedCategories.Add(new { name = dto.Name, reason = "Kategori sudah ada." });
                    continue;
                }

                categoriesToAdd.Add(new Category { Name = dto.Name });
            }

            if (categoriesToAdd.Any())
            {
                try
                {
                    _context.Categories.AddRange(categoriesToAdd);
                    _context.SaveChanges();

                    foreach (var newCategory in categoriesToAdd)
                    {
                        createdCategories.Add(new CategoryResponseDto
                        {
                            Id = newCategory.Id,
                            Name = newCategory.Name
                        });
                    }
                }
                catch (Exception ex)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, new ApiErrorResponse
                    {
                        success = false,
                        message = "Terjadi kesalahan saat menyimpan data ke database.",
                        errors = ex.Message
                    });
                }
            }

            var response = new ApiResponse<object>
            {
                success = true,
                message = "Proses pembuatan kategori selesai.",
                data = new
                {
                    created = createdCategories,
                    skipped = skippedCategories
                }
            };

            return Ok(response);
        }


        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<CategoryResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public IActionResult UpdateCategory(int id, [FromBody] UpdateCategoryDto updateCategoryDto)
        {
            try
            {
                if (updateCategoryDto == null || string.IsNullOrEmpty(updateCategoryDto.Name))
                {
                    return BadRequest(new ApiErrorResponse
                    {
                        success = false,
                        message = "Nama kategori tidak boleh kosong",
                    });
                }
                var category = _context.Categories.Find(id);
                if (category == null)
                {
                    return NotFound(new ApiErrorResponse
                    {
                        success = false,
                        message = "Kategori tidak ditemukan"
                    });
                }
                category.Name = updateCategoryDto.Name;
                _context.SaveChanges();
                var response = new CategoryResponseDto
                {
                    Id = category.Id,
                    Name = category.Name
                };
                return Ok(new ApiResponse<CategoryResponseDto>
                {
                    success = true,
                    message = "Berhasil memperbarui kategori",
                    data = response
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiErrorResponse
                {
                    success = false,
                    message = "Data invalid",
                    errors = ex.Message
                });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public IActionResult DeleteCategory(int id)
        {
            try
            {
                var category = _context.Categories.Find(id);
                if (category == null)
                {
                    return NotFound(new ApiErrorResponse
                    {
                        success = false,
                        message = "Kategori tidak ditemukan"
                    });
                }
                _context.Categories.Remove(category);
                _context.SaveChanges();
                return Ok(new ApiResponse<string>
                {
                    success = true,
                    message = "Berhasil menghapus kategori",
                    data = $"Kategori dengan ID {id} telah dihapus"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiErrorResponse
                {
                    success = false,
                    message = "Terjadi kesalahan saat menghapus kategori",
                    errors = ex.Message
                });
            }
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using olx_be_api.Data;
using olx_be_api.DTO;
using olx_be_api.Helpers;
using olx_be_api.Models;
using System.Linq;

namespace olx_be_api.Controllers
{
    [Route("api/product")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly Random _random = new Random();

        public ProductController(AppDbContext context)
        {
            _context = context;
        }

        private async Task<long> GenerateProductId()
        {
            long newId;
            bool exists;
            do
            {
                newId = _random.NextInt64(100_000_000L, 1_000_000_000L);
                exists = await _context.Products.AnyAsync(p => p.Id == newId);
            } while (exists);
            return newId;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<ProductResponseDTO>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetProducts(
            [FromQuery] string? searchTerm,
            [FromQuery] int? categoryId,
            [FromQuery] int? minPrice,
            [FromQuery] int? maxPrice,
            [FromQuery] string? sortBy,
            [FromQuery] bool isDescending = false)
        {
            var query = _context.Products
                .Include(p => p.ProductImages)
                .Where(p => !p.IsSold)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(p => p.Title.Contains(searchTerm) || (p.Description != null && p.Description.Contains(searchTerm)));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            if (minPrice.HasValue)
            {
                query = query.Where(p => p.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= maxPrice.Value);
            }

            switch (sortBy?.ToLower())
            {
                case "price":
                    query = isDescending ? query.OrderByDescending(p => p.Price) : query.OrderBy(p => p.Price);
                    break;
                case "date":
                default:
                    query = isDescending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt);
                    break;
            }

            var products = await query.Select(p => new ProductResponseDTO
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description,
                Price = p.Price,
                CategoryId = p.CategoryId ?? 0,
                LocationId = p.LocationId ?? 0, // Anda mungkin ingin mengubah ini
                IsSold = p.IsSold,
                CreatedAt = p.CreatedAt,
                Images = p.ProductImages.Select(i => i.ImageUrl).ToList()
            }).ToListAsync();

            return Ok(new ApiResponse<List<ProductResponseDTO>> { success = true, message = "Products retrieved successfully", data = products });
        }


        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<ProductResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProductById(long id)
        {
            var product = await _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound(new ApiErrorResponse { success = false, message = "Product not found" });
            }

            var response = new ProductResponseDTO
            {
                Id = product.Id,
                Title = product.Title,
                Description = product.Description,
                Price = product.Price,
                CategoryId = product.CategoryId ?? 0,
                LocationId = 0, // Ganti sesuai kebutuhan
                IsSold = product.IsSold,
                CreatedAt = product.CreatedAt,
                Images = product.ProductImages.Select(i => i.ImageUrl).ToList()
            };

            return Ok(new ApiResponse<ProductResponseDTO> { success = true, message = "Product retrieved successfully", data = response });
        }

        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<ProductResponseDTO>), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductDTO productDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiErrorResponse { success = false, message = "Invalid data", errors = ModelState });
            }

            var userId = User.GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized();
            }

            var newProduct = new Product
            {
                Id = await GenerateProductId(),
                Title = productDTO.Title,
                Description = productDTO.Description,
                Price = productDTO.Price,
                CategoryId = productDTO.CategoryId,
                LocationId = productDTO.LocationId != 0 ? productDTO.LocationId : (int?)null, // Asumsi 0 berarti null
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                ExpiredAt = DateTime.UtcNow.AddDays(30) // Default masa aktif iklan 30 hari
            };

            // Handling images
            if (productDTO.Images != null && productDTO.Images.Any())
            {
                bool isFirst = true;
                foreach (var imageUrl in productDTO.Images)
                {
                    newProduct.ProductImages.Add(new ProductImage
                    {
                        ImageUrl = imageUrl,
                        IsCover = isFirst
                    });
                    isFirst = false;
                }
            }

            _context.Products.Add(newProduct);
            await _context.SaveChangesAsync();

            var response = new ProductResponseDTO
            {
                Id = newProduct.Id,
                Title = newProduct.Title,
                Price = newProduct.Price,
                // ... map sisa properti
            };

            return CreatedAtAction(nameof(GetProductById), new { id = newProduct.Id }, new ApiResponse<ProductResponseDTO> { success = true, message = "Product created", data = response });
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateProduct(long id, [FromBody] UpdateProductDTO productDTO)
        {
            var userId = User.GetUserId();
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

            if (product == null)
            {
                return Forbid(); // Atau NotFound
            }

            product.Title = productDTO.Title ?? product.Title;
            product.Description = productDTO.Description ?? product.Description;
            product.Price = productDTO.Price ?? product.Price;
            product.CategoryId = (int?)(productDTO.CategoryId ?? product.CategoryId);

            _context.Products.Update(product);
            await _context.SaveChangesAsync();

            return Ok(new ApiResponse<string> { success = true, message = "Product updated successfully." });
        }

        [HttpPatch("{id}/sold")]
        [Authorize]
        public async Task<IActionResult> MarkAsSold(long id)
        {
            var userId = User.GetUserId();
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

            if (product == null)
            {
                return Forbid();
            }

            product.IsSold = true;
            _context.Products.Update(product);
            await _context.SaveChangesAsync();

            return Ok(new ApiResponse<string> { success = true, message = "Product marked as sold." });
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteProduct(long id)
        {
            var userId = User.GetUserId();
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

            if (product == null)
            {
                return Forbid();
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return Ok(new ApiResponse<string> { success = true, message = "Product deleted successfully." });
        }
    }
}
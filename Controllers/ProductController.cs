using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using olx_be_api.Data;
using olx_be_api.DTO;
using olx_be_api.Helpers;
using olx_be_api.Models;
using olx_be_api.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace olx_be_api.Controllers
{
    [Route("api/products")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IGeocodingService _geocodingService;
        private readonly IStorageService _storageService;
        private readonly Random _random = new Random();

        public ProductController(AppDbContext context, IGeocodingService geocodingService, IStorageService storageService)
        {
            _context = context;
            _geocodingService = geocodingService;
            _storageService = storageService;
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
        public async Task<IActionResult> GetProducts(
            [FromQuery] string? searchTerm, [FromQuery] int? categoryId, [FromQuery] int? cityId,
            [FromQuery] int? minPrice, [FromQuery] int? maxPrice, [FromQuery] string? sortBy,
            [FromQuery] bool isDescending = false)
        {
            var query = _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.Category)
                .Include(p => p.Location).ThenInclude(l => l.City)
                .Include(p => p.Location).ThenInclude(l => l.Province)
                .Include(p => p.Location).ThenInclude(l => l.District)
                .Where(p => !p.IsSold)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(p => p.Title.Contains(searchTerm) || (p.Description != null && p.Description.Contains(searchTerm)));
            }
            if (categoryId.HasValue) query = query.Where(p => p.CategoryId == categoryId.Value);
            if (cityId.HasValue) query = query.Where(p => p.Location.CityId == cityId.Value);
            if (minPrice.HasValue) query = query.Where(p => p.Price >= minPrice.Value);
            if (maxPrice.HasValue) query = query.Where(p => p.Price <= maxPrice.Value);

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
                Description = p.Description!,
                Price = p.Price,
                IsSold = p.IsSold,
                CreatedAt = p.CreatedAt,
                Images = p.ProductImages.Select(i => i.ImageUrl).ToList(),
                CategoryId = p.CategoryId ?? 0,
                CategoryName = p.Category != null ? p.Category.Name : "N/A",
                ProvinceId = p.Location.ProvinceId,
                ProvinceName = p.Location.Province!.name,
                CityId = p.Location.CityId,
                CityName = p.Location.City!.Name,
                DistrictId = p.Location.DistrictId,
                DistrictName = p.Location.District!.Name,
            }).ToListAsync();

            return Ok(new ApiResponse<List<ProductResponseDTO>> { success = true, message = "Products retrieved successfully", data = products });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductById(long id)
        {
            var product = await _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.User)
                .Include(p => p.Category)
                .Include(p => p.Location).ThenInclude(l => l.City)
                .Include(p => p.Location).ThenInclude(l => l.Province)
                .Include(p => p.Location).ThenInclude(l => l.District)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound(new ApiErrorResponse { success = false, message = "Product not found" });
            }

            var response = new ProductResponseDTO
            {
                Id = product.Id,
                Title = product.Title,
                Description = product.Description!,
                Price = product.Price,
                IsSold = product.IsSold,
                CreatedAt = product.CreatedAt,
                Images = product.ProductImages.Select(i => i.ImageUrl).ToList(),
                CategoryId = product.CategoryId ?? 0,
                CategoryName = product.Category != null ? product.Category.Name : "N/A",
                ProvinceId = product.Location?.ProvinceId,
                ProvinceName = product.Location?.Province?.name,
                CityId = product.Location?.CityId,
                CityName = product.Location?.City?.Name,
                DistrictId = product.Location?.DistrictId,
                DistrictName = product.Location?.District?.Name,
            };

            return Ok(new ApiResponse<ProductResponseDTO> { success = true, message = "Product retrieved successfully", data = response });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateProduct([FromForm] CreateProductDTO productDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiErrorResponse { success = false, message = "Invalid data", errors = ModelState });
            }

            var userId = User.GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new ApiErrorResponse { success = false, message = "Unauthorized" });
            }

            if (productDTO.Images == null || !productDTO.Images.Any())
            {
                return BadRequest(new ApiErrorResponse { message = "Minimal harus ada satu gambar yang diunggah." });
            }

            Guid? locationId = null;

            var newProduct = new Product
            {
                Id = await GenerateProductId(),
                Title = productDTO.Title,
                Description = productDTO.Description,
                Price = productDTO.Price,
                CategoryId = productDTO.CategoryId,
                LocationId = locationId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                ExpiredAt = DateTime.UtcNow.AddDays(30)
            };

            bool isFirst = true;
            foreach (var imageFile in productDTO.Images)
            {
                try
                {
                    var imageUrl = await _storageService.UploadAsync(imageFile, "product-images");
                    newProduct.ProductImages.Add(new ProductImage
                    {
                        ImageUrl = imageUrl,
                        IsCover = isFirst
                    });
                    isFirst = false;
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new ApiErrorResponse { message = $"Gagal mengunggah gambar: {ex.Message}" });
                }
            }

            _context.Products.Add(newProduct);
            await _context.SaveChangesAsync();

            var actionResult = await GetProductById(newProduct.Id);
            if (actionResult is OkObjectResult okResult)
            {
                return CreatedAtAction(nameof(GetProductById), new { id = newProduct.Id }, okResult.Value);
            }
            return StatusCode(201, new { success = true, message = "Produk berhasil dibuat." });
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateProduct(long id, [FromForm] UpdateProductDTO productDTO)
        {
            var userId = User.GetUserId();
            var product = await _context.Products
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

            if (product == null)
            {
                return Forbid();
            }

            product.Title = productDTO.Title ?? product.Title;
            product.Description = productDTO.Description ?? product.Description;
            product.Price = productDTO.Price ?? product.Price;
            product.CategoryId = productDTO.CategoryId ?? product.CategoryId;

            if (productDTO.UrlsToDelete != null && productDTO.UrlsToDelete.Any())
            {
                var imagesToDelete = product.ProductImages
                    .Where(pi => productDTO.UrlsToDelete.Contains(pi.ImageUrl))
                    .ToList();

                foreach (var img in imagesToDelete)
                {
                    await _storageService.DeleteAsync(img.ImageUrl, "product-images");
                    _context.ProductImages.Remove(img);
                }
            }

            if (productDTO.NewImages != null && productDTO.NewImages.Any())
            {
                foreach (var imageFile in productDTO.NewImages)
                {
                    var newImageUrl = await _storageService.UploadAsync(imageFile, "product-images");
                    product.ProductImages.Add(new ProductImage { ImageUrl = newImageUrl, IsCover = false });
                }
            }

            if (!product.ProductImages.Any(pi => pi.IsCover))
            {
                var firstImage = product.ProductImages.FirstOrDefault();
                if (firstImage != null) firstImage.IsCover = true;
            }

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
            var product = await _context.Products
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

            if (product == null)
            {
                return Forbid();
            }

            foreach (var image in product.ProductImages)
            {
                try
                {
                    await _storageService.DeleteAsync(image.ImageUrl, "product-images");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Gagal menghapus file {image.ImageUrl}: {ex.Message}");
                }
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return Ok(new ApiResponse<string> { success = true, message = "Berhasil menghapus produk/iklan" });
        }
    }
}
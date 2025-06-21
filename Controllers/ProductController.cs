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
        [ProducesResponseType(typeof(ApiResponse<List<ProductResponseDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllProducts([FromQuery] bool isMyAds = false)
        {
            if (!Guid.TryParse(User.GetUserId().ToString(), out var userId))
            {
                return Unauthorized(new ApiErrorResponse { success = false, message = "Invalid user ID." });
            }

            var expiredProducts = await _context.Products
                .Where(p => p.IsActive && p.ExpiredAt < DateTime.UtcNow)
                .ToListAsync();

            if (expiredProducts.Any())
            {
                foreach (var product in expiredProducts)
                {
                    product.IsActive = false;
                }
                _context.Products.UpdateRange(expiredProducts);
                await _context.SaveChangesAsync();
            }

            var query = _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.Category)
                .Include(p => p.Location).ThenInclude(l => l.Province)
                .Include(p => p.Location).ThenInclude(l => l.City)
                .Include(p => p.Location).ThenInclude(l => l.District)
                .OrderByDescending(p => p.CreatedAt)
                .Where(p => p.IsActive && !p.IsSold);

            if (isMyAds)
            {
                query = query.Where(p => p.UserId == userId);
            }
            else
            {
                query = query.Where(p => p.UserId != userId);
            }

            var products = await query.Select(p => new ProductResponseDTO
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description ?? string.Empty,
                Price = p.Price,
                IsSold = p.IsSold,
                CreatedAt = p.CreatedAt,
                CategoryId = p.CategoryId ?? 0,
                CategoryName = p.Category != null ? p.Category.Name : "N/A",
                Images = p.ProductImages.Select(i => i.ImageUrl).ToList(),
                ProvinceId = p.Location != null && p.Location.Province != null ? p.Location.Province.id : null,
                ProvinceName = p.Location != null && p.Location.Province != null ? p.Location.Province.name : null,
                CityId = p.Location != null && p.Location.City != null ? p.Location.City.Id : null,
                CityName = p.Location != null && p.Location.City != null ? p.Location.City.Name : null,
                DistrictId = p.Location != null && p.Location.District != null ? p.Location.District.Id : null,
                DistrictName = p.Location != null && p.Location.District != null ? p.Location.District.Name : null
            }).ToListAsync();

            return Ok(new ApiResponse<List<ProductResponseDTO>> { success = true, message = "Products retrieved successfully", data = products });
        }

        [HttpGet("search")]
        [ProducesResponseType(typeof(ApiResponse<List<ProductResponseDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SearchProducts([FromQuery] string? searchTerm, [FromQuery] string? cityName)
        {
            var query = _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.Category)
                .Include(p => p.Location).ThenInclude(l => l.Province)
                .Include(p => p.Location).ThenInclude(l => l.City)
                .Include(p => p.Location).ThenInclude(l => l.District)
                .Where(p => p.IsActive && !p.IsSold)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(p => p.Title.Contains(searchTerm));
            }

            if (!string.IsNullOrWhiteSpace(cityName))
            {
                query = query.Where(p => p.Location.City != null && p.Location.City.Name.Contains(cityName, StringComparison.OrdinalIgnoreCase));
            }

            var products = await query.Select(p => new ProductResponseDTO
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description ?? string.Empty,
                Price = p.Price,
                IsSold = p.IsSold,
                CreatedAt = p.CreatedAt,
                CategoryId = p.CategoryId ?? 0,
                CategoryName = p.Category != null ? p.Category.Name : "N/A",
                Images = p.ProductImages.Select(i => i.ImageUrl).ToList(),
                ProvinceId = p.Location != null && p.Location.Province != null ? p.Location.Province.id : null,
                ProvinceName = p.Location != null && p.Location.Province != null ? p.Location.Province.name : null,
                CityId = p.Location != null && p.Location.City != null ? p.Location.City.Id : null,
                CityName = p.Location != null && p.Location.City != null ? p.Location.City.Name : null,
                DistrictId = p.Location != null && p.Location.District != null ? p.Location.District.Id : null,
                DistrictName = p.Location != null && p.Location.District != null ? p.Location.District.Name : null
            }).ToListAsync();

            return Ok(new ApiResponse<List<ProductResponseDTO>> { success = true, message = $"Search results for '{searchTerm}' retrieved successfully", data = products });
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<ProductResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProductById(long id)
        {
            var product = await _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.User)
                .Include(p => p.Category)
                .Include(p => p.Location).ThenInclude(l => l.City)
                .Include(p => p.Location).ThenInclude(l => l.Province)
                .Include(p => p.Location).ThenInclude(l => l.District)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound(new ApiErrorResponse { success = false, message = "Product not found" });
            }

            var response = new ProductResponseDTO
            {
                Id = product.Id,
                Title = product.Title,
                Description = product.Description ?? string.Empty,
                Price = product.Price,
                IsSold = product.IsSold,
                CreatedAt = product.CreatedAt,
                CategoryId = product.CategoryId ?? 0,
                CategoryName = product.Category != null ? product.Category.Name : "N/A",
                Images = product.ProductImages.Select(i => i.ImageUrl).ToList(),
                ProvinceId = product.Location?.Province?.id,
                ProvinceName = product.Location?.Province?.name,
                CityId = product.Location?.City?.Id,
                CityName = product.Location?.City?.Name,
                DistrictId = product.Location?.District?.Id,
                DistrictName = product.Location?.District?.Name
            };

            return Ok(new ApiResponse<ProductResponseDTO> { success = true, message = "Product retrieved successfully", data = response });
        }

        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<ProductResponseDTO>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateProduct([FromForm] CreateProductDTO productDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiErrorResponse { success = false, message = "Invalid data", errors = ModelState });
            }

            if (!Guid.TryParse(User .GetUserId().ToString(), out var userId) || await _context.Users.FindAsync(userId) == null)
            {
                return Unauthorized(new ApiErrorResponse { success = false, message = "User not found." });
            }

            if (productDTO.Images == null || !productDTO.Images.Any())
            {
                return BadRequest(new ApiErrorResponse { message = "At least one image must be uploaded." });
            }

            var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == productDTO.CategoryId);
            if (category == null)
            {
                return BadRequest(new ApiErrorResponse { success = false, message = "Invalid category ID." });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            var imageUrls = new List<string>();
            try
            {
                var locationDetails = await _geocodingService.GetLocationDetailsFromCoordinates(productDTO.Latitude, productDTO.Longitude);
                
                Province? province = null;
                City? city = null;
                District? district = null;

                if (!string.IsNullOrWhiteSpace(locationDetails?.Province))
                {
                    province = await _context.Provinces.FirstOrDefaultAsync(p => p.name.Equals(locationDetails.Province, StringComparison.OrdinalIgnoreCase));
                    if (province == null)
                    {
                        province = new Province { name = locationDetails.Province };
                        _context.Provinces.Add(province);
                        await _context.SaveChangesAsync();
                    }
                }

                if (!string.IsNullOrWhiteSpace(locationDetails?.City) && province != null)
                {
                    city = await _context.Cities.FirstOrDefaultAsync(c => c.Name.Equals(locationDetails.City, StringComparison.OrdinalIgnoreCase) && c.ProvinceId == province.id);
                    if (city == null)
                    {
                        city = new City { Name = locationDetails.City, ProvinceId = province.id };
                        _context.Cities.Add(city);
                        await _context.SaveChangesAsync();
                    }
                }

                if (!string.IsNullOrWhiteSpace(locationDetails?.District) && city != null)
                {
                    district = await _context.Districts.FirstOrDefaultAsync(d => d.Name.Equals(locationDetails.District, StringComparison.OrdinalIgnoreCase) && d.CityId == city.Id);
                    if (district == null)
                    {
                        district = new District { Name = locationDetails.District, CityId = city.Id };
                        _context.Districts.Add(district);
                        await _context.SaveChangesAsync();
                    }
                }
                
                var location = new Location
                {
                    Latitude = productDTO.Latitude,
                    Longitude = productDTO.Longitude,
                    ProvinceId = province?.id,
                    CityId = city?.Id,
                    DistrictId = district?.Id
                };

                _context.Locations.Add(location);
                await _context.SaveChangesAsync();

                var newProduct = new Product
                {
                    Id = await GenerateProductId(),
                    Title = productDTO.Title,
                    Description = productDTO.Description,
                    Price = productDTO.Price,
                    CategoryId = productDTO.CategoryId,
                    UserId = userId,
                    LocationId = location.Id,
                    CreatedAt = DateTime.UtcNow,
                    ExpiredAt = DateTime.UtcNow.AddDays(30),
                    IsActive = true,
                    IsSold = false
                };

                foreach (var imageFile in productDTO.Images)
                {
                    var imageUrl = await _storageService.UploadAsync(imageFile, "product-images");
                    imageUrls.Add(imageUrl);
                }

                bool isFirst = true;
                foreach (var url in imageUrls)
                {
                    newProduct.ProductImages.Add(new ProductImage { ImageUrl = url, IsCover = isFirst });
                    isFirst = false;
                }

                _context.Products.Add(newProduct);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var createdProduct = await _context.Products
                    .Include(p => p.ProductImages)
                    .Include(p => p.Category)
                    .Include(p => p.Location)
                    .ThenInclude(l => l.Province)
                    .Include(p => p.Location)
                    .ThenInclude(l => l.City)
                    .Include(p => p.Location)
                    .ThenInclude(l => l.District)
                    .FirstOrDefaultAsync(p => p.Id == newProduct.Id);

                var responseDto = new ProductResponseDTO
                {
                    Id = createdProduct!.Id,
                    Title = createdProduct.Title,
                    Description = createdProduct.Description ?? string.Empty,
                    Price = createdProduct.Price,
                    IsSold = createdProduct.IsSold,
                    CreatedAt = createdProduct.CreatedAt,
                    CategoryId = createdProduct.CategoryId ?? 0,
                    CategoryName = createdProduct.Category?.Name ?? "N/A",
                    Images = createdProduct.ProductImages.Select(i => i.ImageUrl).ToList(),
                    ProvinceId = createdProduct.Location?.Province?.id,
                    ProvinceName = createdProduct.Location?.Province?.name,
                    CityId = createdProduct.Location?.City?.Id,
                    CityName = createdProduct.Location?.City?.Name,
                    DistrictId = createdProduct.Location?.District?.Id,
                    DistrictName = createdProduct.Location?.District?.Name
                };

                return CreatedAtAction(
                    nameof(GetProductById), 
                    new { id = createdProduct.Id }, 
                    new ApiResponse<ProductResponseDTO>
                    {
                        success = true,
                        message = "Product created successfully",
                        data = responseDto
                    });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                foreach (var url in imageUrls)
                {
                    await _storageService.DeleteAsync(url, "product-images");
                }
                return StatusCode(500, new ApiErrorResponse { message = $"Error creating product: {ex.Message}" });
            }
        }

        [HttpPut("{id}")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateProduct(long id, [FromForm] UpdateProductDTO productDTO)
        {
            if (!Guid.TryParse(User.GetUserId().ToString(), out var userId))
            {
                return Unauthorized(new ApiErrorResponse { success = false, message = "Invalid user ID." });
            }
            
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
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> MarkAsSold(long id)
        {
            if (!Guid.TryParse(User.GetUserId().ToString(), out var userId))
            {
                return Unauthorized(new ApiErrorResponse { success = false, message = "Invalid user ID." });
            }
            
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

        [HttpPatch("{id}/deactivate")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeactivateProduct(long id)
        {
            if (!Guid.TryParse(User.GetUserId().ToString(), out var userId))
            {
                return Unauthorized(new ApiErrorResponse { success = false, message = "Invalid user ID." });
            }
            
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

            if (product == null)
            {
                return NotFound(new ApiErrorResponse { success = false, message = "Product not found or you do not have permission to modify it." });
            }

            product.IsActive = false;
            _context.Products.Update(product);
            await _context.SaveChangesAsync();

            return Ok(new ApiResponse<string> { success = true, message = "Product deactivated successfully." });
        }

        [HttpDelete("{id}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteProduct(long id)
        {
            if (!Guid.TryParse(User.GetUserId().ToString(), out var userId))
            {
                return Unauthorized(new ApiErrorResponse { success = false, message = "Invalid user ID." });
            }
            
            var product = await _context.Products
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

            if (product == null)
            {
                return Forbid();
            }

            foreach (var image in product.ProductImages)
            {
                await _storageService.DeleteAsync(image.ImageUrl, "product-images");
            }
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}

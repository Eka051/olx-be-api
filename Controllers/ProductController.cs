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
        [ProducesResponseType(typeof(ApiResponse<List<ProductResponseDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllProducts()
        {
            var expiredProducts = await _context.Products
                .Where(p => p.IsActive && p.ExpiredAt < DateTime.UtcNow)
                .ToListAsync();

            foreach (var product in expiredProducts)
            {
                product.IsActive = false;
            }

            if (expiredProducts.Any())
            {
                foreach (var product in expiredProducts)
                {
                    product.IsActive = false;
                }
                _context.Products.UpdateRange(expiredProducts);
                await _context.SaveChangesAsync();
            }

            var products = await _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.Location).ThenInclude(l => l.City)
                .OrderByDescending(p => p.CreatedAt)
                .Where(p => p.IsActive && !p.IsSold)
                .Select(p => new ProductResponseDTO
                {
                    Id = p.Id,
                    Title = p.Title,
                    Description = p.Description!,
                    Price = p.Price,
                    IsSold = p.IsSold,
                    CreatedAt = p.CreatedAt,
                    Images = p.ProductImages.Select(i => i.ImageUrl).ToList(),
                    CityId = p.Location.CityId,
                    CityName = p.Location.City!.Name,
                }).ToListAsync();

            return Ok(new ApiResponse<List<ProductResponseDTO>> { success = true, message = "Products retrieved successfully", data = products });
        }

        [HttpGet("search")]
        [ProducesResponseType(typeof(ApiResponse<List<ProductResponseDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SearchProducts(
            [FromQuery] string? searchTerm, [FromQuery] string? cityName)
        {
            var query = _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.Location).ThenInclude(l => l.City)
                .Where(p => p.IsActive && !p.IsSold)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(p => p.Title.Contains(searchTerm));
            }

            if (!string.IsNullOrWhiteSpace(cityName))
            {
                query = query.Where(p => p.Location.City!.Name.Contains(cityName, StringComparison.OrdinalIgnoreCase));
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
                CityId = p.Location.CityId,
                CityName = p.Location.City!.Name,
            }).ToListAsync();

            return Ok(new ApiResponse<List<ProductResponseDTO>> { success = true, message = $"Hasil pencarian produk {searchTerm} berhasil", data = products });
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<ProductResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
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
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateProduct([FromForm] CreateProductDTO productDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiErrorResponse { success = false, message = "Invalid data", errors = ModelState });
            }

            var userId = User.GetUserId();
            var user = await _context.Users.FindAsync(userId);
            
            if (user == null)
            {
                return Unauthorized(new ApiErrorResponse { success = false, message = "User not found." });
            }            if (productDTO.Images == null || !productDTO.Images.Any())
            {
                return BadRequest(new ApiErrorResponse { message = "Minimal harus ada satu gambar yang diunggah." });
            }

            var locationDetails = await _geocodingService.GetLocationDetailsFromCoordinates(productDTO.Latitude, productDTO.Longitude);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var location = await GetOrCreateLocationAsync(locationDetails, productDTO.Latitude, productDTO.Longitude);

                var newProduct = new Product
                {
                    Id = await GenerateProductId(),
                    Title = productDTO.Title,
                    Description = productDTO.Description,
                    Price = productDTO.Price,
                    CategoryId = productDTO.CategoryId,
                    UserId = userId,
                    Location = location,
                    CreatedAt = DateTime.UtcNow,
                    ExpiredAt = DateTime.UtcNow.AddDays(30)
                };

                var imageUrls = new List<string>();
                foreach (var imageFile in productDTO.Images)
                {
                    try
                    {
                        var imageUrl = await _storageService.UploadAsync(imageFile, "product-images");
                        imageUrls.Add(imageUrl);
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        return StatusCode(500, new ApiErrorResponse { message = $"Gagal mengunggah gambar: {ex.Message}" });
                    }
                }

                bool isFirst = true;
                foreach (var url in imageUrls)
                {                    newProduct.ProductImages.Add(new ProductImage { ImageUrl = url, IsCover = isFirst });
                    isFirst = false;
                }

                _context.Products.Add(newProduct);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var result = await GetProductById(newProduct.Id);
                var createdResult = result as OkObjectResult;

                return CreatedAtAction(nameof(GetProductById), new { id = newProduct.Id }, createdResult!.Value);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new ApiErrorResponse { message = $"Error creating product: {ex.Message}" });
            }
        }

        [HttpPut("{id}")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
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
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
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
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
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
               await _storageService.DeleteAsync(image.ImageUrl, "product-images");
            }            
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private async Task<Location> GetOrCreateLocationAsync(LocationDetails? locationDetails, double latitude, double longitude)
        {
            Province? province = null;
            City? city = null;
            District? district = null;

            if (!string.IsNullOrWhiteSpace(locationDetails?.Province))
            {
                province = await _context.Provinces.FirstOrDefaultAsync(p => p.name == locationDetails.Province);
                if (province == null)
                {
                    var maxProvinceId = await _context.Provinces.AnyAsync() 
                        ? await _context.Provinces.MaxAsync(p => p.id) 
                        : 0;
                    
                    province = new Province 
                    { 
                        id = maxProvinceId + 1,
                        name = locationDetails.Province 
                    };
                    _context.Provinces.Add(province);
                }
            }

            if (!string.IsNullOrWhiteSpace(locationDetails?.City) && province != null)
            {
                city = await _context.Cities.FirstOrDefaultAsync(c => c.Name == locationDetails.City && c.ProvinceId == province.id);
                if (city == null)
                {
                    var maxCityId = await _context.Cities.AnyAsync() 
                        ? await _context.Cities.MaxAsync(c => c.Id) 
                        : 0;
                    
                    city = new City
                    {
                        Id = maxCityId + 1,
                        Name = locationDetails.City,
                        ProvinceId = province.id
                    };
                    _context.Cities.Add(city);
                }
            }

            if (!string.IsNullOrWhiteSpace(locationDetails?.District) && city != null)
            {
                district = await _context.Districts.FirstOrDefaultAsync(d => d.Name == locationDetails.District && d.CityId == city.Id);
                if (district == null)
                {
                    var maxDistrictId = await _context.Districts.AnyAsync() 
                        ? await _context.Districts.MaxAsync(d => d.Id) 
                        : 0;
                    
                    district = new District
                    {
                        Id = maxDistrictId + 1,
                        Name = locationDetails.District,
                        CityId = city.Id
                    };
                    _context.Districts.Add(district);
                }
            }

            return new Location
            {
                Latitude = latitude,
                Longitude = longitude,
                Province = province,
                City = city,
                District = district
            };
        }
    }
}
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
        private readonly Random _random = new Random();

        public ProductController(AppDbContext context, IGeocodingService geocodingService)
        {
            _context = context;
            _geocodingService = geocodingService;
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
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProducts(
            [FromQuery] string? searchTerm,
            [FromQuery] int? categoryId,
            [FromQuery] int? cityId,
            [FromQuery] int? minPrice,
            [FromQuery] int? maxPrice,
            [FromQuery] string? sortBy,
            [FromQuery] bool isDescending = false)
        {
            var query = _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.Location).ThenInclude(l => l.City)
                .Include(p => p.Location).ThenInclude(l => l.Province)
                .Include(p => p.Location).ThenInclude(l => l.District)
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

            if (cityId.HasValue)
            {
                query = query.Where(p => p.Location.CityId == cityId.Value);
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
                Description = p.Description!,
                Price = p.Price,
                CategoryId = p.CategoryId ?? 0,
                IsSold = p.IsSold,
                CreatedAt = p.CreatedAt,
                Images = p.ProductImages.Select(i => i.ImageUrl).ToList(),
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
        [ProducesResponseType(typeof(ApiResponse<ProductResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProductById(long id)
        {
            var product = await _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.User)
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
                CategoryId = product.CategoryId ?? 0,
                IsSold = product.IsSold,
                CreatedAt = product.CreatedAt,
                Images = product.ProductImages.Select(i => i.ImageUrl).ToList(),
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
        [ProducesResponseType(typeof(ApiResponse<ProductResponseDTO>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductDTO productDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiErrorResponse { success = false, message = "Invalid data", errors = ModelState });
            }

            var existingProduct = await _context.Products
                .FirstOrDefaultAsync(p => p.Title == productDTO.Title && p.UserId == User.GetUserId());
            if (existingProduct != null)
            {
                return Conflict(new ApiErrorResponse { 
                    success = false, 
                    message = $"Produk dengan nama {productDTO.Title} sudah ada. Ganti dengan nama produk lain " 
                });
            }

                var userId = User.GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new ApiErrorResponse { success = false, message = "Unauthorized" });
            }

            Guid? locationId = null;
            if (productDTO.Latitude != 0 && productDTO.Longitude != 0)
            {
                var locationDetails = await _geocodingService.GetLocationDetailsFromCoordinates(
                    productDTO.Latitude,
                    productDTO.Longitude
                );

                if (locationDetails != null)
                {
                    if (!string.IsNullOrEmpty(locationDetails.Province) && !string.IsNullOrEmpty(locationDetails.City))
                    {
                        var city = await _context.Cities.FirstOrDefaultAsync(c =>
                            c.Name == locationDetails.City || c.Name == locationDetails.City.Replace("Kabupaten ", "").Replace("Kota ", ""));

                        var province = await _context.Provinces.FirstOrDefaultAsync(p => p.name == locationDetails.Province);

                        if (city != null && province != null)
                        {
                            var district = !string.IsNullOrEmpty(locationDetails.District)
                                ? await _context.Districts.FirstOrDefaultAsync(d => d.Name == locationDetails.District && d.CityId == city.Id)
                                : null;

                            var existingLocation = await _context.Locations.FirstOrDefaultAsync(l =>
                                l.CityId == city.Id && l.ProvinceId == province.id && l.DistrictId == district!.Id);

                            if (existingLocation != null)
                            {
                                locationId = existingLocation.Id;
                            }
                            else
                            {
                                var newLocation = new Location
                                {
                                    ProvinceId = province.id,
                                    CityId = city.Id,
                                    DistrictId = district?.Id,
                                    Latitude = productDTO.Latitude,
                                    Longitude = productDTO.Longitude
                                };
                                _context.Locations.Add(newLocation);
                                await _context.SaveChangesAsync();
                                locationId = newLocation.Id;
                            }
                        }
                    }
                }
            }

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

            var responseDto = await GetProductById(newProduct.Id);

            return CreatedAtAction(nameof(GetProductById), new { id = newProduct.Id }, responseDto);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<UpdateProductDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        [Authorize]
        public async Task<IActionResult> UpdateProduct(long id, [FromBody] UpdateProductDTO productDTO)
        {
            var userId = User.GetUserId();
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

            if (product == null)
            {
                return Forbid();
            }

            var existingProduct = await _context.Products
                .FirstOrDefaultAsync(p => p.Title == productDTO.Title && p.UserId == userId && p.Id != id);
            if (existingProduct != null)
            {
                return Conflict(new ApiErrorResponse
                {
                    success = false,
                    message = $"Produk dengan nama {productDTO.Title} sudah ada. Ganti dengan nama produk lain."
                });
            }

                product.Title = productDTO.Title ?? product.Title;
            product.Description = productDTO.Description ?? product.Description;
            product.Price = productDTO.Price ?? product.Price;
            product.CategoryId = productDTO.CategoryId;

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
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
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

            return Ok(new ApiResponse<string> { success = true, message = "Berhasil menghapus produk/iklan" });
        }
    }
}
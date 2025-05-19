using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using olx_be_api.Data;
using olx_be_api.DTO;
using olx_be_api.Models;
using System.Security.Claims;

namespace olx_be_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductsController(AppDbContext context)
        {
            _context = context;
        }
        
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductResponseDto>>> GetProducts()
        {
            var products = await _context.Products
                .Include(p => p.Seller)
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Where(p => p.IsAvailable)
                .Select(p => new ProductResponseDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Description = p.Description,
                    Price = p.Price,
                    SellerId = p.SellerId,
                    SellerName = p.Seller.Name,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category.Name,
                    Location = p.Location,
                    IsAvailable = p.IsAvailable,
                    ViewCount = p.ViewCount,
                    Images = p.Images.Select(i => new ProductImageResponseDto
                    {
                        Id = i.Id,
                        ProductId = i.ProductId,
                        ImageUrl = i.ImageUrl,
                        IsMain = i.IsMain,
                        CreatedAt = i.CreatedAt
                    }).ToList(),
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                })
                .ToListAsync();

            return Ok(products);
        }
        
        [HttpGet("category/{categoryId}")]
        public async Task<ActionResult<IEnumerable<ProductResponseDto>>> GetProductsByCategory(Guid categoryId)
        {
            var products = await _context.Products
                .Include(p => p.Seller)
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Where(p => p.CategoryId == categoryId && p.IsAvailable)
                .Select(p => new ProductResponseDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Description = p.Description,
                    Price = p.Price,
                    SellerId = p.SellerId,
                    SellerName = p.Seller.Name,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category.Name,
                    Location = p.Location,
                    IsAvailable = p.IsAvailable,
                    ViewCount = p.ViewCount,
                    Images = p.Images.Select(i => new ProductImageResponseDto
                    {
                        Id = i.Id,
                        ProductId = i.ProductId,
                        ImageUrl = i.ImageUrl,
                        IsMain = i.IsMain,
                        CreatedAt = i.CreatedAt
                    }).ToList(),
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                })
                .ToListAsync();

            return Ok(products);
        }
        
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<ProductResponseDto>>> GetProductsByUser(Guid userId)
        {
            var products = await _context.Products
                .Include(p => p.Seller)
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Where(p => p.SellerId == userId)
                .Select(p => new ProductResponseDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Description = p.Description,
                    Price = p.Price,
                    SellerId = p.SellerId,
                    SellerName = p.Seller.Name,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category.Name,
                    Location = p.Location,
                    IsAvailable = p.IsAvailable,
                    ViewCount = p.ViewCount,
                    Images = p.Images.Select(i => new ProductImageResponseDto
                    {
                        Id = i.Id,
                        ProductId = i.ProductId,
                        ImageUrl = i.ImageUrl,
                        IsMain = i.IsMain,
                        CreatedAt = i.CreatedAt
                    }).ToList(),
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                })
                .ToListAsync();

            return Ok(products);
        }
        
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductResponseDto>> GetProduct(Guid id)
        {
            var product = await _context.Products
                .Include(p => p.Seller)
                .Include(p => p.Category)
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            // Increment view count
            product.ViewCount++;
            await _context.SaveChangesAsync();

            return new ProductResponseDto
            {
                Id = product.Id,
                Title = product.Title,
                Description = product.Description,
                Price = product.Price,
                SellerId = product.SellerId,
                SellerName = product.Seller.Name,
                CategoryId = product.CategoryId,
                CategoryName = product.Category.Name,
                Location = product.Location,
                IsAvailable = product.IsAvailable,
                ViewCount = product.ViewCount,
                Images = product.Images.Select(i => new ProductImageResponseDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ImageUrl = i.ImageUrl,
                    IsMain = i.IsMain,
                    CreatedAt = i.CreatedAt
                }).ToList(),
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt
            };
        }
        
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<ProductResponseDto>> CreateProduct([FromBody] CreateProductDto createProductDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized();
            }

            if (!await _context.Categories.AnyAsync(c => c.Id == createProductDto.CategoryId))
            {
                return BadRequest("Invalid category ID");
            }

            var product = new Product
            {
                Title = createProductDto.Title,
                Description = createProductDto.Description,
                Price = createProductDto.Price,
                SellerId = userGuid,
                CategoryId = createProductDto.CategoryId,
                Location = createProductDto.Location
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Add product images if provided
            if (createProductDto.ImageUrls != null && createProductDto.ImageUrls.Count > 0)
            {
                var images = createProductDto.ImageUrls.Select((url, index) => new ProductImage
                {
                    ProductId = product.Id,
                    ImageUrl = url,
                    IsMain = index == 0 // First image is main by default
                }).ToList();

                _context.ProductImages.AddRange(images);
                await _context.SaveChangesAsync();
            }

            // Get the created product with related data
            var createdProduct = await _context.Products
                .Include(p => p.Seller)
                .Include(p => p.Category)
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == product.Id);

            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, new ProductResponseDto
            {
                Id = createdProduct!.Id,
                Title = createdProduct.Title,
                Description = createdProduct.Description,
                Price = createdProduct.Price,
                SellerId = createdProduct.SellerId,
                SellerName = createdProduct.Seller.Name,
                CategoryId = createdProduct.CategoryId,
                CategoryName = createdProduct.Category.Name,
                Location = createdProduct.Location,
                IsAvailable = createdProduct.IsAvailable,
                ViewCount = createdProduct.ViewCount,
                Images = createdProduct.Images.Select(i => new ProductImageResponseDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ImageUrl = i.ImageUrl,
                    IsMain = i.IsMain,
                    CreatedAt = i.CreatedAt
                }).ToList(),
                CreatedAt = createdProduct.CreatedAt,
                UpdatedAt = createdProduct.UpdatedAt
            });
        }
        
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductDto updateProductDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized();
            }

            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            if (product.SellerId != userGuid)
            {
                return Forbid();
            }

            if (updateProductDto.Title != null)
                product.Title = updateProductDto.Title;
                
            if (updateProductDto.Description != null)
                product.Description = updateProductDto.Description;
                
            if (updateProductDto.Price.HasValue)
                product.Price = updateProductDto.Price.Value;
                
            if (updateProductDto.CategoryId.HasValue)
            {
                if (!await _context.Categories.AnyAsync(c => c.Id == updateProductDto.CategoryId))
                {
                    return BadRequest("Invalid category ID");
                }
                product.CategoryId = updateProductDto.CategoryId.Value;
            }
                
            if (updateProductDto.Location != null)
                product.Location = updateProductDto.Location;
                
            if (updateProductDto.IsAvailable.HasValue)
                product.IsAvailable = updateProductDto.IsAvailable.Value;

            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }
        
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteProduct(Guid id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized();
            }

            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            if (product.SellerId != userGuid)
            {
                return Forbid();
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
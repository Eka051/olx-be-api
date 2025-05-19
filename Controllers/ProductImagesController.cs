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
    [Authorize]
    public class ProductImagesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductImagesController(AppDbContext context)
        {
            _context = context;
        }
        
        [HttpGet("product/{productId}")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<ProductImageResponseDto>>> GetProductImages(Guid productId)
        {
            var images = await _context.ProductImages
                .Where(i => i.ProductId == productId)
                .Select(i => new ProductImageResponseDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ImageUrl = i.ImageUrl,
                    IsMain = i.IsMain,
                    CreatedAt = i.CreatedAt
                })
                .ToListAsync();

            return Ok(images);
        }
        
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<ProductImageResponseDto>> GetProductImage(Guid id)
        {
            var image = await _context.ProductImages.FindAsync(id);

            if (image == null)
            {
                return NotFound();
            }

            return new ProductImageResponseDto
            {
                Id = image.Id,
                ProductId = image.ProductId,
                ImageUrl = image.ImageUrl,
                IsMain = image.IsMain,
                CreatedAt = image.CreatedAt
            };
        }
        
        [HttpPost]
        public async Task<ActionResult<ProductImageResponseDto>> CreateProductImage([FromBody] CreateProductImageDto createProductImageDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized();
            }

            var product = await _context.Products.FindAsync(createProductImageDto.ProductId);

            if (product == null)
            {
                return BadRequest("Product not found");
            }

            if (product.SellerId != userGuid)
            {
                return Forbid();
            }

            var image = new ProductImage
            {
                ProductId = createProductImageDto.ProductId,
                ImageUrl = createProductImageDto.ImageUrl,
                IsMain = createProductImageDto.IsMain
            };

            // If this image is set as main, unset main for other images of this product
            if (image.IsMain)
            {
                var existingMainImages = await _context.ProductImages
                    .Where(i => i.ProductId == image.ProductId && i.IsMain)
                    .ToListAsync();
                    
                foreach (var mainImage in existingMainImages)
                {
                    mainImage.IsMain = false;
                }
            }

            _context.ProductImages.Add(image);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProductImage), new { id = image.Id }, new ProductImageResponseDto
            {
                Id = image.Id,
                ProductId = image.ProductId,
                ImageUrl = image.ImageUrl,
                IsMain = image.IsMain,
                CreatedAt = image.CreatedAt
            });
        }
        
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProductImage(Guid id, [FromBody] UpdateProductImageDto updateProductImageDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized();
            }

            var image = await _context.ProductImages
                .Include(i => i.Product)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (image == null)
            {
                return NotFound();
            }

            if (image.Product.SellerId != userGuid)
            {
                return Forbid();
            }

            if (updateProductImageDto.IsMain.HasValue && updateProductImageDto.IsMain.Value)
            {
                // Unset main for other images of this product
                var existingMainImages = await _context.ProductImages
                    .Where(i => i.ProductId == image.ProductId && i.IsMain && i.Id != id)
                    .ToListAsync();
                    
                foreach (var mainImage in existingMainImages)
                {
                    mainImage.IsMain = false;
                }
                
                image.IsMain = true;
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }
        
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProductImage(Guid id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized();
            }

            var image = await _context.ProductImages
                .Include(i => i.Product)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (image == null)
            {
                return NotFound();
            }

            if (image.Product.SellerId != userGuid)
            {
                return Forbid();
            }

            _context.ProductImages.Remove(image);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
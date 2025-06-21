using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using olx_be_api.Data;
using olx_be_api.DTO;
using olx_be_api.Helpers;
using olx_be_api.Models;

namespace olx_be_api.Controllers
{
    [Route("api/favorites")]
    [ApiController]
    public class FavoriteController : ControllerBase
    {
        private readonly AppDbContext _context;
        public FavoriteController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<ProductResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetFavoriteProducts()
        {
            var userId = User.GetUserId();

            var favoriteProducts = await _context.Favorites
                .Where(f => f.UserId == userId)
                .Include(f => f.Product).ThenInclude(p => p.ProductImages)
                .Include(f => f.Product).ThenInclude(p => p.Location).ThenInclude(l => l.City)
                .Select(f => f.Product)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new ProductResponseDTO
                {
                    Id = p.Id,
                    Title = p.Title,
                    Price = p.Price,
                    CreatedAt = p.CreatedAt,
                    Images = p.ProductImages.Select(i => i.ImageUrl).ToList(),
                    CityName = p.Location.City!.Name,
                    IsSold = p.IsSold
                })
                .ToListAsync();

            if (favoriteProducts == null || !favoriteProducts.Any())
            {
                return NotFound(new ApiErrorResponse
                {
                    message = "Produk favorit tidak ditemukan"
                });
            }

            return Ok(new ApiResponse<List<ProductResponseDTO>>
            {
                success = true,
                message = "Produk favorit berhasil diambil",
                data = favoriteProducts
            });
        }

        [HttpPost("{productId}")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<CreateFavoriteDTO>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AddToFavorites(long productId)
        {
            var userId = User.GetUserId();
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return NotFound(new ApiErrorResponse
                {
                    message = "Produk tidak ditemukan"
                });
            }
            var existingFavorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == productId);
            if (existingFavorite != null)
            {
                return BadRequest(new ApiErrorResponse
                {
                    message = "Produk sudah ada di favorit"
                });
            }
            var favorite = new Favorite
            {
                UserId = userId,
                ProductId = productId
            };
            _context.Favorites.Add(favorite);
            await _context.SaveChangesAsync();

            var response = new FavoriteResponseDTO
            {
                Id = favorite.Id,
                UserId = favorite.UserId,
                ProductId = favorite.ProductId,
                CreatedAt = favorite.CreatedAt
            };

            return CreatedAtAction(nameof(GetFavoriteProducts), new { id = favorite.Id }, new ApiResponse<FavoriteResponseDTO>
            {
                success = true,
                message = "Produk berhasil ditambahkan ke favorit",
                data = response
            });
        }

        [HttpDelete("{productId}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RemoveFromFavorites(long productId)
        {
            var userId = User.GetUserId();
            var favorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == productId);
            if (favorite == null)
            {
                return NotFound(new ApiErrorResponse
                {
                    message = "Produk tidak ditemukan di favorit"
                });
            }
            _context.Favorites.Remove(favorite);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}

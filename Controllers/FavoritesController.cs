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
    public class TransactionsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TransactionsController(AppDbContext context)
        {
            _context = context;
        }
        
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TransactionResponseDto>>> GetTransactions()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized();
            }

            var transactions = await _context.Transactions
                .Include(t => t.Product)
                .Include(t => t.Buyer)
                .Include(t => t.Seller)
                .Where(t => t.BuyerId == userGuid || t.SellerId == userGuid)
                .Select(t => new TransactionResponseDto
                {
                    Id = t.Id,
                    ProductId = t.ProductId,
                    ProductTitle = t.Product.Title,
                    Amount = t.Amount,
                    BuyerId = t.BuyerId,
                    BuyerName = t.Buyer.Name,
                    SellerId = t.SellerId,
                    SellerName = t.Seller.Name,
                    Status = t.Status,
                    Notes = t.Notes,
                    CompletedAt = t.CompletedAt,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();

            return transactions;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TransactionResponseDto>> GetTransaction(Guid id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized();
            }

            var transaction = await _context.Transactions
                .Include(t => t.Product)
                .Include(t => t.Buyer)
                .Include(t => t.Seller)
                .FirstOrDefaultAsync(t => t.Id == id && (t.BuyerId == userGuid || t.SellerId == userGuid));

            if (transaction == null)
            {
                return NotFound();
            }

            return new TransactionResponseDto
            {
                Id = transaction.Id,
                ProductId = transaction.ProductId,
                ProductTitle = transaction.Product.Title,
                Amount = transaction.Amount,
                BuyerId = transaction.BuyerId,
                BuyerName = transaction.Buyer.Name,
                SellerId = transaction.SellerId,
                SellerName = transaction.Seller.Name,
                Status = transaction.Status,
                Notes = transaction.Notes,
                CompletedAt = transaction.CompletedAt,
                CreatedAt = transaction.CreatedAt
            };
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FavoritesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FavoritesController(AppDbContext context)
        {
            _context = context;
        }
        
        [HttpGet]
        public async Task<ActionResult<IEnumerable<FavoriteResponseDto>>> GetFavorites()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized();
            }

            var favorites = await _context.Favorites
                .Include(f => f.Product)
                    .ThenInclude(p => p.Images)
                .Where(f => f.UserId == userGuid)
                .Select(f => new FavoriteResponseDto
                {
                    Id = f.Id,
                    UserId = f.UserId,
                    ProductId = f.ProductId,
                    ProductTitle = f.Product.Title,
                    ProductPrice = f.Product.Price,
                    MainImageUrl = f.Product.Images
                        .FirstOrDefault(i => i.IsMain)?.ImageUrl ?? 
                        f.Product.Images.FirstOrDefault()?.ImageUrl,
                    CreatedAt = f.CreatedAt
                })
                .ToListAsync();

            return Ok(favorites);
        }
        
        [HttpGet("{id}")]
        public async Task<ActionResult<FavoriteResponseDto>> GetFavorite(Guid id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized();
            }

            var favorite = await _context.Favorites
                .Include(f => f.Product)
                    .ThenInclude(p => p.Images)
                .FirstOrDefaultAsync(f => f.Id == id && f.UserId == userGuid);

            if (favorite == null)
            {
                return NotFound();
            }

            return new FavoriteResponseDto
            {
                Id = favorite.Id,
                UserId = favorite.UserId,
                ProductId = favorite.ProductId,
                ProductTitle = favorite.Product.Title,
                ProductPrice = favorite.Product.Price,
                MainImageUrl = favorite.Product.Images
                    .FirstOrDefault(i => i.IsMain)?.ImageUrl ?? 
                    favorite.Product.Images.FirstOrDefault()?.ImageUrl,
                CreatedAt = favorite.CreatedAt
            };
        }
        
        [HttpPost]
        public async Task<ActionResult<FavoriteResponseDto>> CreateFavorite([FromBody] CreateFavoriteDto createFavoriteDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized();
            }

            // Check if product exists
            var product = await _context.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == createFavoriteDto.ProductId);

            if (product == null)
            {
                return BadRequest("Product not found");
            }

            // Check if already favorited
            var existingFavorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userGuid && f.ProductId == createFavoriteDto.ProductId);

            if (existingFavorite != null)
            {
                return BadRequest("Product is already in favorites");
            }

            var favorite = new Favorite
            {
                UserId = userGuid,
                ProductId = createFavoriteDto.ProductId
            };

            _context.Favorites.Add(favorite);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetFavorite), new { id = favorite.Id }, new FavoriteResponseDto
            {
                Id = favorite.Id,
                UserId = favorite.UserId,
                ProductId = favorite.ProductId,
                ProductTitle = product.Title,
                ProductPrice = product.Price,
                MainImageUrl = product.Images
                    .FirstOrDefault(i => i.IsMain)?.ImageUrl ?? 
                    product.Images.FirstOrDefault()?.ImageUrl,
                CreatedAt = favorite.CreatedAt
            });
        }
        
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFavorite(Guid id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized();
            }

            var favorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.Id == id && f.UserId == userGuid);

            if (favorite == null)
            {
                return NotFound();
            }

            _context.Favorites.Remove(favorite);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        
        [HttpGet("product/{productId}")]
        public async Task<ActionResult<bool>> IsFavorite(Guid productId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized();
            }

            var isFavorite = await _context.Favorites
                .AnyAsync(f => f.UserId == userGuid && f.ProductId == productId);

            return Ok(isFavorite);
        }
        
        [HttpDelete("product/{productId}")]
        public async Task<IActionResult> RemoveFavoriteByProductId(Guid productId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized();
            }

            var favorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userGuid && f.ProductId == productId);

            if (favorite == null)
            {
                return NotFound();
            }

            _context.Favorites.Remove(favorite);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
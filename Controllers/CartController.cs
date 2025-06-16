using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using olx_be_api.Data;
using olx_be_api.DTO;
using olx_be_api.Helpers;
using olx_be_api.Models;

namespace olx_be_api.Controllers
{
    [Route("api/cart")]
    [ApiController]
    [Authorize]
    public class CartController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CartController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<CartResponseDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCart()
        {
            var userId = User.GetUserId();
            var cartItems = await _context.CartItems
                .Where(ci => ci.UserId == userId)
                .Include(ci => ci.AdPackage)
                .Include(ci => ci.Product)
                .ThenInclude(p => p.User)
                .ToListAsync();

            if (!cartItems.Any())
            {
                return Ok(new ApiResponse<List<CartResponseDTO>>
                {
                    success = true,
                    message = "Keranjang kosong",
                    data = new List<CartResponseDTO>()
                });
            }

            var response = cartItems.Select(ci => new CartResponseDTO
            {
                Id = ci.Id,
                AdPackageId = ci.AdPackageId,
                AdPackageName = ci.AdPackage.Name,
                Quantity = ci.Quantity,
                ProductId = ci.Product.Id,
                ProductTitle = ci.Product.Title,
                UserId = ci.UserId,
                UserName = ci.User.Name!
            }).ToList();

            return Ok(new ApiResponse<List<CartResponseDTO>> { success = true, message = "Berhasil mengambil data keranjang", data = response });
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AddToCart([FromBody] CartCreateDTO cartCreateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiErrorResponse { success = false, message = "Data tidak valid", errors = ModelState });
            }

            var userId = User.GetUserId();
            var adPackage = await _context.AdPackages.FindAsync(cartCreateDto.AdPackageId);
            if (adPackage == null)
            {
                return NotFound(new ApiErrorResponse { success = false, message = "Paket iklan tidak ditemukan" });
            }

            var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == cartCreateDto.ProductId && p.UserId == userId);
            if (product == null)
            {
                return NotFound(new ApiErrorResponse { success = false, message = "Produk tidak ditemukan atau bukan milik Anda." });
            }

            var existingCartItem = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.UserId == userId && ci.ProductId == cartCreateDto.ProductId && ci.AdPackageId == cartCreateDto.AdPackageId);

            if (existingCartItem != null)
            {
                existingCartItem.Quantity += cartCreateDto.Quantity;
                _context.CartItems.Update(existingCartItem);
            }
            else
            {
                var cartItem = new CartItem
                {
                    UserId = userId,
                    AdPackageId = cartCreateDto.AdPackageId,
                    ProductId = cartCreateDto.ProductId,
                    Quantity = cartCreateDto.Quantity
                };
                _context.CartItems.Add(cartItem);
            }

            await _context.SaveChangesAsync();
            return Created("", new ApiResponse<string> { success = true, message = "Berhasil menambahkan ke keranjang" });
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RemoveFromCart(Guid id)
        {
            var userId = User.GetUserId();
            var cartItem = await _context.CartItems.FirstOrDefaultAsync(ci => ci.Id == id && ci.UserId == userId);

            if (cartItem == null)
            {
                return NotFound(new ApiErrorResponse { success = false, message = "Item keranjang tidak ditemukan" });
            }

            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();

            return Ok(new ApiResponse<string> { success = true, message = "Berhasil menghapus item dari keranjang" });
        }
    }
}
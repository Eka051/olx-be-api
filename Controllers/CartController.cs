using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCart()
        {
            var userId = User.GetUserId();
            var cartItems = await _context.CartItems
                .Where(ci => ci.UserId == userId)
                .Include(ci => ci.AdPackage)
                .Include(ci => ci.Product)
                .ToListAsync();

            if (!cartItems.Any())
            {
                return NotFound(new ApiErrorResponse
                {
                    success = false,
                    message = "Keranjang kosong"
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

            return Ok(new ApiResponse<List<CartResponseDTO>>
            {
                success = true,
                message = "Berhasil mengambil data keranjang",
                data = response
            });
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<CartResponseDTO>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AddToCart([FromBody] CartCreateDTO cartCreateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiErrorResponse
                {
                    success = false,
                    message = "Data tidak valid",
                    errors = ModelState
                });
            }

            var userId = User.GetUserId();
            var adPackage = await _context.AdPackages.FindAsync(cartCreateDto.AdPackageId);

            if (adPackage == null)
            {
                return BadRequest(new ApiErrorResponse
                {
                    success = false,
                    message = "Paket iklan tidak ditemukan"
                });
            }

            var cartItem = new CartItem
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                AdPackageId = cartCreateDto.AdPackageId,
                Quantity = cartCreateDto.Quantity,
                AdPackage = adPackage
            };

            _context.CartItems.Add(cartItem);
            await _context.SaveChangesAsync();

            var response = new CartResponseDTO
            {
                Id = cartItem.Id,
                AdPackageId = cartItem.AdPackageId,
                AdPackageName = adPackage.Name,
                Quantity = cartItem.Quantity,
                ProductId = cartItem.Product.Id,
                ProductTitle = cartItem.Product.Title,
                UserId = cartItem.UserId,
                UserName = cartItem.User.Name!
            };

            return CreatedAtAction(nameof(GetCart), new { id = cartItem.Id }, new ApiResponse<CartResponseDTO>
            {
                success = true,
                message = "Berhasil menambahkan ke keranjang",
                data = response
            });
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RemoveFromCart(Guid id)
        {
            var userId = User.GetUserId();
            var cartItem = await _context.CartItems.FirstOrDefaultAsync(ci => ci.Id == id && ci.UserId == userId);

            if (cartItem == null)
            {
                return NotFound(new ApiErrorResponse
                {
                    success = false,
                    message = "Item keranjang tidak ditemukan"
                });
            }

            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();

            return Ok(new ApiResponse<string>
            {
                success = true,
                message = "Berhasil menghapus item dari keranjang",
                data = $"Item dengan ID {id} telah dihapus dari keranjang"
            });
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using olx_be_api.Data;
using olx_be_api.Helpers;
using olx_be_api.Models;
using olx_be_api.Models.Enums;
using olx_be_api.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace olx_be_api.Controllers
{
    [Route("api/payments")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMidtransService _midtransService;
        private readonly IConfiguration _configuration;

        public PaymentController(AppDbContext context, IMidtransService midtransService, IConfiguration configuration)
        {
            _context = context;
            _midtransService = midtransService;
            _configuration = configuration;
        }

        [HttpPost("premium-subscriptions/{id}/checkout")]
        [Authorize]
        public async Task<IActionResult> CheckoutPremiumAsync(int id)
        {
            var userId = User.GetUserId();
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return Unauthorized(new ApiErrorResponse { message = "Pengguna tidak ditemukan." });
            }

            var package = await _context.PremiumPackages.FindAsync(id);
            if (package == null || !package.IsActive)
            {
                return NotFound(new ApiErrorResponse { success = false, message = "Paket Premium tidak ditemukan" });
            }

            var transaction = new Transaction
            {
                UserId = userId,
                InvoiceNumber = $"INV-PREMIUM-{DateTime.UtcNow.Ticks}",
                Amount = package.Price,
                Status = TransactionStatus.Pending,
                Type = TransactionType.PremiumSubscription,
                ReferenceId = package.Id.ToString(),
                Details = JsonSerializer.Serialize(new List<TransactionItemDetail> { new TransactionItemDetail { Price = package.Price, Quantity = 1 } })
            };

            var finishRedirectUrl = _configuration["Midtrans:FinishRedirectUrl"];
            if (string.IsNullOrEmpty(finishRedirectUrl))
            {
                return StatusCode(500, new ApiErrorResponse { message = "FinishRedirectUrl is not configured on the server." });
            }

            var midtransRequest = new MidtransRequest
            {
                InvoiceNumber = transaction.InvoiceNumber,
                Amount = transaction.Amount,
                CustomerDetails = new CustomerDetails { FirstName = user.Name ?? "Pengguna OLX", Email = user.Email! },
                ItemDetails = new List<ItemDetails> { new ItemDetails { Id = package.Id.ToString(), Name = package.Description ?? $"Premium {package.DurationDays} Hari", Price = package.Price, Quantity = 1 } },
                Callbacks = new MidtransCallbacks { Finish = finishRedirectUrl }
            };

            var midtransResponse = await _midtransService.CreateSnapTransaction(midtransRequest);
            if (!midtransResponse.IsSuccess)
            {
                return StatusCode(500, new ApiErrorResponse { message = midtransResponse.ErrorMessage! });
            }

            transaction.PaymentUrl = midtransResponse.RedirectUrl;
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetPaymentByInvoice),
                new { invoiceNumber = transaction.InvoiceNumber },
                new ApiResponse<object>
                {
                    success = true,
                    message = "URL pembayaran berhasil dibuat",
                    data = new
                    {
                        paymentUrl = midtransResponse.RedirectUrl,
                        finishUrl = finishRedirectUrl
                    }
                }
            );
        }

        [HttpPost("cart/checkout")]
        [Authorize]
        public async Task<IActionResult> CheckoutCartAsync()
        {
            var userId = User.GetUserId();
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return Unauthorized(new ApiErrorResponse { message = "Pengguna tidak ditemukan." });
            }

            var cartItems = await _context.CartItems
                .Where(ci => ci.UserId == userId)
                .Include(ci => ci.AdPackage)
                .Include(ci => ci.Product)
                .ToListAsync();

            if (!cartItems.Any())
            {
                return NotFound(new ApiErrorResponse { success = false, message = "Keranjang Anda kosong." });
            }
            int totalAmount = cartItems.Sum(item => item.AdPackage.Price * item.Quantity);

            var itemDetails = cartItems.Select(item => new ItemDetails
            {
                Id = item.AdPackageId.ToString(),
                Name = $"Iklan '{item.AdPackage.Name}' untuk '{item.Product.Title}'",
                Price = item.AdPackage.Price,
                Quantity = item.Quantity
            }).ToList();

            var transactionDetails = cartItems.Select(item => new TransactionItemDetail
            {
                AdPackageId = item.AdPackageId,
                ProductId = item.ProductId,
                Price = item.AdPackage.Price,
                Quantity = item.Quantity
            }).ToList();

            var transaction = new Transaction
            {
                UserId = userId,
                InvoiceNumber = $"INV-CART-{DateTime.UtcNow.Ticks}",
                Amount = totalAmount,
                Status = TransactionStatus.Pending,
                Type = TransactionType.AdPackagePurchase,
                ReferenceId = Guid.NewGuid().ToString(),
                Details = JsonSerializer.Serialize(transactionDetails)
            };

            var finishRedirectUrl = _configuration["Midtrans:FinishRedirectUrl"];
            if (string.IsNullOrEmpty(finishRedirectUrl))
            {
                return StatusCode(500, new ApiErrorResponse { message = "FinishRedirectUrl is not configured on the server." });
            }

            var midtransRequest = new MidtransRequest
            {
                InvoiceNumber = transaction.InvoiceNumber,
                Amount = totalAmount,
                CustomerDetails = new CustomerDetails { FirstName = user.Name ?? "Pengguna OLX", Email = user.Email! },
                ItemDetails = itemDetails,
                Callbacks = new MidtransCallbacks { Finish = finishRedirectUrl }
            };

            var midtransResponse = await _midtransService.CreateSnapTransaction(midtransRequest);
            if (!midtransResponse.IsSuccess)
            {
                return StatusCode(500, new ApiErrorResponse { message = midtransResponse.ErrorMessage! });
            }

            transaction.PaymentUrl = midtransResponse.RedirectUrl;
            _context.Transactions.Add(transaction);
            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetPaymentByInvoice),
                new { invoiceNumber = transaction.InvoiceNumber },
                new ApiResponse<object>
                {
                    success = true,
                    message = "URL pembayaran berhasil dibuat",
                    data = new
                    {
                        paymentUrl = midtransResponse.RedirectUrl,
                        finishUrl = finishRedirectUrl
                    }
                });
        }

        [HttpGet("{invoiceNumber}")]
        [Authorize]
        public async Task<IActionResult> GetPaymentByInvoice(string invoiceNumber)
        {
            var userId = User.GetUserId();
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.InvoiceNumber == invoiceNumber && t.UserId == userId);

            if (transaction == null)
            {
                return NotFound(new ApiErrorResponse { message = "Transaksi tidak ditemukan" });
            }

            return Ok(new ApiResponse<Transaction> { success = true, message = "Transaksi berhasil ditemukan", data = transaction });
        }
    }
}

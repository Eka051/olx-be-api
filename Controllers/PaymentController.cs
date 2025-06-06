using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using olx_be_api.Data;
using olx_be_api.Helpers;
using olx_be_api.Models;
using olx_be_api.Services;

namespace olx_be_api.Controllers
{
    [Route("api/payment")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IDokuService _dokuService;
        public PaymentController(AppDbContext context, IDokuService dokuService)
        {
            _context = context;
            _dokuService = dokuService;
        }

        [HttpPost("checkout-premium/{packageId}")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CheckoutPremiumAsync(int packageId)
        {
            var userId = User.GetUserId();
            var package = _context.PremiumPackages.Find(packageId);

            if (package == null || !package.IsActive)
            {
                return NotFound(new ApiErrorResponse
                {
                    success = false,
                    message = "Paket Premium tidak ditemukan"
                });
            }

            var transaction = new Transaction
            {
                UserId = userId,
                InvoiceNumber = $"INV-PREMIUM-{DateTime.UtcNow.Ticks}",
                Amount = package.Price,
                Status = TransactionStatus.Pending,
                Type = TransactionType.PremiumSubscription,
                ReferenceId = package.Id.ToString()
            };

            var dokuRequest = new DokuPaymentRequest
            {
                InvoiceNumber = transaction.InvoiceNumber,
                Amount = transaction.Amount,
                ProductName = $"Langganan Premium {package.Name}",
            };

            var dokuResponse = await _dokuService.CreatePayment(dokuRequest);

            if (!dokuResponse.IsSuccess)
            {
                return StatusCode(500, new ApiErrorResponse { message = dokuResponse.ErrorMessage! });
            }

            transaction.PaymentUrl = dokuResponse.PaymentUrl;
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            return Ok(new ApiResponse<string> { success = true, message = "URL pembayaran berhasil dibuat", data = dokuResponse.PaymentUrl });
        }
    }
}

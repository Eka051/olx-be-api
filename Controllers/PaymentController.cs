using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using olx_be_api.Data;
using olx_be_api.Helpers;
using olx_be_api.Models;
using olx_be_api.Models.Enums;
using olx_be_api.Services;
using System.Text.Json;

namespace olx_be_api.Controllers
{
    [Route("api/payments")]
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

        [HttpPost("premium-subscriptions/{id}/checkout")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
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
                ReferenceId = package.Id.ToString()
            };

            var dokuRequest = new DokuPaymentRequest
            {
                InvoiceNumber = transaction.InvoiceNumber,
                Amount = transaction.Amount,
                CustomerName = user.Name ?? "Pengguna OLX",
                CustomerEmail = user.Email!,
                LineItems = new List<DokuLineItem>
                {
                    new DokuLineItem { Name = package.Description ?? $"Premium {package.DurationDays} Hari", Price = package.Price, Quantity = 1 }
                }
            };

            var dokuResponse = await _dokuService.CreatePayment(dokuRequest);
            if (!dokuResponse.IsSuccess)
            {
                return StatusCode(500, new ApiErrorResponse { message = dokuResponse.ErrorMessage! });
            }

            transaction.PaymentUrl = dokuResponse.PaymentUrl;
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPaymentByInvoice), new { invoiceNumber = transaction.InvoiceNumber },
                new ApiResponse<string> { success = true, message = "URL pembayaran berhasil dibuat", data = dokuResponse.PaymentUrl });
        }

        [HttpPost("cart/checkout")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
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

            var lineItems = cartItems.Select(item => new DokuLineItem
            {
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

            var dokuRequest = new DokuPaymentRequest
            {
                InvoiceNumber = transaction.InvoiceNumber,
                Amount = totalAmount,
                CustomerName = user.Name ?? "Pengguna OLX",
                CustomerEmail = user.Email!,
                LineItems = lineItems
            };

            var dokuResponse = await _dokuService.CreatePayment(dokuRequest);
            if (!dokuResponse.IsSuccess)
            {
                return StatusCode(500, new ApiErrorResponse { message = dokuResponse.ErrorMessage! });
            }

            transaction.PaymentUrl = dokuResponse.PaymentUrl;
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();
            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPaymentByInvoice), new { invoiceNumber = transaction.InvoiceNumber },
                new ApiResponse<string> { success = true, message = "URL pembayaran berhasil dibuat", data = dokuResponse.PaymentUrl });
        }

        [HttpPost("webhooks/doku")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DokuNotification()
        {
            using var reader = new StreamReader(Request.Body);
            var requestBody = await reader.ReadToEndAsync();
            var notification = JsonSerializer.Deserialize<JsonElement>(requestBody);

            var transactionStatus = notification.GetProperty("transaction").GetProperty("status").GetString();
            var invoiceNumber = notification.GetProperty("order").GetProperty("invoice_number").GetString();

            var transaction = await _context.Transactions
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.InvoiceNumber == invoiceNumber);

            if (transaction != null && transaction.Status == TransactionStatus.Pending)
            {
                if (transactionStatus == "SUCCESS")
                {
                    transaction.Status = TransactionStatus.Success;
                    transaction.PaidAt = DateTime.UtcNow;

                    if (transaction.Type == TransactionType.PremiumSubscription)
                    {
                        var package = await _context.PremiumPackages.FindAsync(int.Parse(transaction.ReferenceId));
                        if (package != null)
                        {
                            var user = transaction.User;
                            user.ProfileType = ProfileType.Premium;
                            var newExpiry = (user.PremiumUntil.HasValue && user.PremiumUntil > DateTime.UtcNow)
                                ? user.PremiumUntil.Value.AddDays(package.DurationDays)
                                : DateTime.UtcNow.AddDays(package.DurationDays);
                            user.PremiumUntil = newExpiry;
                            _context.Users.Update(user);
                        }
                    }
                    else if (transaction.Type == TransactionType.AdPackagePurchase && !string.IsNullOrEmpty(transaction.Details))
                    {
                        var purchasedItems = JsonSerializer.Deserialize<List<TransactionItemDetail>>(transaction.Details);
                        if (purchasedItems != null)
                        {
                            var adPackageIds = purchasedItems.Select(p => p.AdPackageId).Distinct().ToList();
                            var adPackages = await _context.AdPackages
                                .Include(ap => ap.Features)
                                .Where(ap => adPackageIds.Contains(ap.Id))
                                .ToDictionaryAsync(ap => ap.Id);

                            var allProductIds = purchasedItems.Select(item => item.ProductId).ToList();
                            var activeFeaturesForProducts = await _context.ActiveProductFeatures
                                .Where(af => allProductIds.Contains(af.ProductId))
                                .ToListAsync();

                            var newFeaturesToAdd = new List<ActiveProductFeature>();

                            foreach (var item in purchasedItems)
                            {
                                if (adPackages.TryGetValue(item.AdPackageId, out var adPackage))
                                {
                                    foreach (var feature in adPackage.Features)
                                    {
                                        var existingFeature = activeFeaturesForProducts.FirstOrDefault(af =>
                                            af.ProductId == item.ProductId && af.FeatureType == feature.FeatureType);

                                        if (existingFeature != null)
                                        {
                                            if (existingFeature.ExpiryDate.HasValue)
                                            {
                                                existingFeature.ExpiryDate = existingFeature.ExpiryDate > DateTime.UtcNow
                                                    ? existingFeature.ExpiryDate.Value.AddDays(feature.DurationDays)
                                                    : DateTime.UtcNow.AddDays(feature.DurationDays);
                                            }
                                            if (feature.FeatureType == AdFeatureType.Sundul)
                                            {
                                                existingFeature.RemainingQuantity += feature.Quantity;
                                            }
                                        }
                                        else
                                        {
                                            var newActiveFeature = new ActiveProductFeature
                                            {
                                                ProductId = item.ProductId,
                                                FeatureType = feature.FeatureType
                                            };

                                            if (feature.FeatureType == AdFeatureType.Highlight || feature.FeatureType == AdFeatureType.Spotlight)
                                            {
                                                newActiveFeature.ExpiryDate = DateTime.UtcNow.AddDays(feature.DurationDays);
                                            }
                                            else if (feature.FeatureType == AdFeatureType.Sundul)
                                            {
                                                newActiveFeature.RemainingQuantity = feature.Quantity;
                                            }
                                            newFeaturesToAdd.Add(newActiveFeature);
                                        }
                                    }
                                }
                            }

                            if (newFeaturesToAdd.Any())
                            {
                                await _context.ActiveProductFeatures.AddRangeAsync(newFeaturesToAdd);
                            }
                        }
                    }
                }
                else
                {
                    transaction.Status = TransactionStatus.Failed;
                }

                _context.Transactions.Update(transaction);
                await _context.SaveChangesAsync();
            }

            return Ok();
        }

        [HttpGet("{invoiceNumber}")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<Transaction>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPaymentByInvoice(string invoiceNumber)
        {
            var userId = User.GetUserId();
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.InvoiceNumber == invoiceNumber && t.UserId == userId);

            if (transaction == null)
            {
                return NotFound(new ApiErrorResponse
                {
                    success = false,
                    message = "Transaksi tidak ditemukan"
                });
            }

            return Ok(new ApiResponse<Transaction>
            {
                success = true,
                message = "Transaksi berhasil ditemukan",
                data = transaction
            });
        }
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using olx_be_api.Data;
using olx_be_api.Helpers;
using olx_be_api.Models;
using olx_be_api.Models.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace olx_be_api.Controllers
{
    [Route("api/notifications")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public NotificationController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<List<Notification>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMyNotifications()
        {
            var userId = User.GetUserId();
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            if (notifications == null || !notifications.Any())
            {
                return NotFound(new ApiResponse<Notification> { success = false, message = "Data Notifikasi tidak ditemukan"});
            }

            return Ok(new ApiResponse<List<Notification>>
            {
                success = true,
                message = "Notifications retrieved successfully",
                data = notifications
            });
        }

        [HttpPost("{id:int}/read")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = User.GetUserId();
            var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == Guid.Parse(id.ToString()) && n.UserId == userId);

            if (notification == null)
            {
                return NotFound(new ApiErrorResponse { message = "Notification not found." });
            }

            notification.IsRead = true;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("unread-count")]
        [Authorize]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = User.GetUserId();
            var count = await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);

            return Ok(new ApiResponse<int>
            {
                success = true,
                message = "Unread count retrieved",
                data = count
            });
        }

        [HttpPost("webhook")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> HandleNotification()
        {
            string requestBody;
            using (var reader = new StreamReader(Request.Body))
            {
                requestBody = await reader.ReadToEndAsync();
            }

            MidtransNotification notification;
            try
            {
                notification = JsonConvert.DeserializeObject<MidtransNotification>(requestBody)!;
                if (notification == null || string.IsNullOrEmpty(notification.order_id))
                {
                    return BadRequest("Invalid notification format.");
                }
            }
            catch (JsonException)
            {
                return BadRequest("Invalid JSON payload.");
            }

            var transaction = await _context.Transactions
                                            .Include(t => t.User)
                                            .FirstOrDefaultAsync(t => t.InvoiceNumber == notification.order_id);

            if (transaction == null)
            {
                return NotFound($"Transaction with Order ID {notification.order_id} not found.");
            }

            if (transaction.Status != TransactionStatus.Pending)
            {
                return Ok("Notification for an already processed transaction received.");
            }

            try
            {
                TransactionStatus statusSebelumnya = transaction.Status;
                TransactionStatus statusTerbaru = transaction.Status;
                string notificationTitle = null!;
                string notificationMessage = null!;

                if (notification.transaction_status == "settlement" || notification.transaction_status == "capture")
                {
                    statusTerbaru = TransactionStatus.Success;
                    notificationTitle = "Pembayaran Berhasil";
                    notificationMessage = $"Pembayaran untuk pesanan #{transaction.InvoiceNumber} telah berhasil.";

                    if (transaction.User != null && transaction.Type == TransactionType.PremiumSubscription && !string.IsNullOrEmpty(transaction.ReferenceId))
                    {
                        if (int.TryParse(transaction.ReferenceId, out int packageId))
                        {
                            var package = await _context.PremiumPackages.FindAsync(packageId);
                            if (package != null)
                            {
                                transaction.User.ProfileType = ProfileType.Premium;
                                var newExpiryDate = (transaction.User.PremiumUntil.HasValue && transaction.User.PremiumUntil > DateTime.UtcNow)
                                    ? transaction.User.PremiumUntil.Value.AddDays(package.DurationDays)
                                    : DateTime.UtcNow.AddDays(package.DurationDays);
                                transaction.User.PremiumUntil = newExpiryDate;
                            }
                        }
                    }
                }
                else if (notification.transaction_status == "expire" || notification.transaction_status == "cancel" || notification.transaction_status == "deny")
                {
                    statusTerbaru = TransactionStatus.Failed;
                    notificationTitle = "Pembayaran Gagal";
                    notificationMessage = $"Pembayaran untuk pesanan #{transaction.InvoiceNumber} gagal atau dibatalkan.";
                }

                if (statusSebelumnya != statusTerbaru)
                {
                    transaction.Status = statusTerbaru;

                    if (notificationTitle != null && transaction.User != null)
                    {
                        var newDbNotification = new Notification
                        {
                            UserId = transaction.UserId,
                            Title = notificationTitle,
                            Message = notificationMessage,
                            IsRead = false,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _context.Notifications.AddAsync(newDbNotification);
                    }

                    await _context.SaveChangesAsync();
                }

                return Ok();
            }
            catch (Exception)
            {
                return StatusCode(500, "Internal server error while updating transaction.");
            }
        }
    }
}
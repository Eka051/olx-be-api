using System.ComponentModel.DataAnnotations;

namespace olx_be_api.Models
{
    public class AdTransaction
    {
        [Key]
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public Guid CartItemId { get; set; }
        [Required]
        public int InvoiceNumber { get; set; }
        public int Amount { get; set; }
        public TransactionStatus Status { get; set; }
        public string PaymentUrl { get; set; } = null!;
        public DateTime? PaidAt { get; set; }
        public DateTime? CreatedAt { get; set; }

        public User User { get; set; } = null!;
        public CartItem CartItem { get; set; } = null!;
    }

    public enum TransactionStatus
    {
        Pending,
        Success,
        Failed,
        Expired,
        Timeout,
        Redirect
    }
}
using System.ComponentModel.DataAnnotations;

namespace olx_be_api.Models
{
    public enum TransactionType
    {
        AdPackagePurchase,
        PremiumSubscription
    }

    public enum TransactionStatus
    {
        Pending,
        Success,
        Failed,
        Expired
    }

    public class Transaction
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public string InvoiceNumber { get; set; } = null!;
        public int Amount { get; set; }
        public TransactionStatus Status { get; set; }
        public TransactionType Type { get; set; }
        public string? Details { get; set; }
        public string ReferenceId { get; set; } = null!;
        public string? PaymentUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PaidAt { get; set; }
        public User User { get; set; } = null!;
    }
}

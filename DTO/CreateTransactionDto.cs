using System.ComponentModel.DataAnnotations;

namespace olx_be_api.DTO
{
    public class CreateTransactionDto
    {
        [Required]
        public Guid ProductId { get; set; }
        
        [MaxLength(1000)]
        public string? Notes { get; set; }
    }

    public class UpdateTransactionDto
    {
        [Required]
        [RegularExpression("^(Completed|Canceled)$", ErrorMessage = "Status must be either 'Completed' or 'Canceled'")]
        public string Status { get; set; } = string.Empty;
        
        [MaxLength(1000)]
        public string? Notes { get; set; }
    }

    public class TransactionResponseDto
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string ProductTitle { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public Guid BuyerId { get; set; }
        public string BuyerName { get; set; } = string.Empty;
        public Guid SellerId { get; set; }
        public string SellerName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
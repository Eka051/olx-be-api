using System;
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
        public string Status { get; set; } = null!; // Completed, Canceled
        
        [MaxLength(1000)]
        public string? Notes { get; set; }
    }

    public class TransactionResponseDto
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string ProductTitle { get; set; } = null!;
        public decimal Amount { get; set; }
        public Guid BuyerId { get; set; }
        public string BuyerName { get; set; } = null!;
        public Guid SellerId { get; set; }
        public string SellerName { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
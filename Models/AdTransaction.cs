namespace olx_be_api.Models
{
    public class AdTransaction
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid CartItemId { get; set; }
        public string Status { get; set; } = null!;
        public string PaymentUrl { get; set; } = null!;
        public DateTime? PaidAt { get; set; }
        public DateTime? CreatedAt { get; set; }

        public User User { get; set; } = null!;
        public CartItem CartItem { get; set; } = null!;
    }
}

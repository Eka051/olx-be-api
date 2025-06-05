namespace olx_be_api.Models
{
    public class ChatRoom
    {
        public Guid Id { get; set; }
        public long ProductId { get; set; }
        public Guid BuyerId { get; set; }
        public Guid SellerId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Product Product { get; set; } = null!;
        public User Buyer { get; set; } = null!;
        public User Seller { get; set; } = null!;
        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}

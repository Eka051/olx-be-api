namespace olx_be_api.DTO
{
    public class CreateChatRoomDto
    {
        public long ProductId { get; set; }
        public string? InitialMessage { get; set; }
    }

    public class ChatRoomResponseDto
    {
        public Guid Id { get; set; }
        public long ProductId { get; set; }
        public string ProductTitle { get; set; } = null!;
        public Guid BuyerId { get; set; }
        public string? BuyerName { get; set; }
        public Guid SellerId { get; set; }
        public string? SellerName { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? LastMessage { get; set; }
        public DateTime LastMessageAt { get; set; }
        public int UnreadCount { get; set; }
    }
}

namespace olx_be_api.DTO
{
    public class NotificationDTO
    {
        public Guid UserId { get; set; }
        public string Title { get; set; } = null!;
        public string? Message { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

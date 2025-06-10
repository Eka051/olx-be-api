using System.ComponentModel.DataAnnotations;

namespace olx_be_api.DTO
{
    public class CreateChatDto
    {
        [Required]
        public Guid ReceiverId { get; set; }
        
        public long? ProductId { get; set; }
        
        [Required]
        [MaxLength(2000)]
        public string InitialMessage { get; set; } = string.Empty;
    }

    public class ChatResponseDto
    {
        public Guid Id { get; set; }
        public Guid InitiatorId { get; set; }
        public string InitiatorName { get; set; } = string.Empty;
        public string? InitiatorProfileImage { get; set; }
        public Guid ReceiverId { get; set; }
        public string ReceiverName { get; set; } = string.Empty;
        public string? ReceiverProfileImage { get; set; }
        public long? ProductId { get; set; }
        public string? ProductTitle { get; set; }
        public string? LastMessageContent { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public List<MessageResponseDto>? Messages { get; set; }
    }
}
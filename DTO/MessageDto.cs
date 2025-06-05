using System;
using System.ComponentModel.DataAnnotations;

namespace olx_be_api.DTO
{
    public class CreateMessageDto
    {
        public int ChatId { get; set; }
        
        [MaxLength(2000)]
        public string Content { get; set; } = null!;
    }

    public class MessageResponseDto
    {
        public int Id { get; set; }
        public int ChatId { get; set; }
        public Guid SenderId { get; set; }
        public string SenderName { get; set; } = null!;
        public string Content { get; set; } = null!;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
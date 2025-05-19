using System;
using System.ComponentModel.DataAnnotations;

namespace olx_be_api.DTO
{
    public class CreateMessageDto
    {
        [Required]
        public Guid ChatId { get; set; }
        
        [Required]
        [MaxLength(2000)]
        public string Content { get; set; } = null!;
    }

    public class MessageResponseDto
    {
        public Guid Id { get; set; }
        public Guid ChatId { get; set; }
        public Guid SenderId { get; set; }
        public string SenderName { get; set; } = null!;
        public string Content { get; set; } = null!;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
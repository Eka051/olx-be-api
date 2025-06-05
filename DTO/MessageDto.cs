using System;
using System.ComponentModel.DataAnnotations;

namespace olx_be_api.DTO
{
    public class CreateMessageDto
    {
        public Guid ChatRoomId { get; set; }
        public string Content { get; set; } = null!;
    }

    public class MessageResponseDto
    {
        public Guid Id { get; set; }
        public string Content { get; set; } = null!;
        public Guid SenderId { get; set; }
        public Guid ChatRoomId { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
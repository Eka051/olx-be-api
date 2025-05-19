using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace olx_be_api.Models
{
    public class Chat
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public Guid InitiatorId { get; set; }
        
        [Required]
        public Guid ReceiverId { get; set; }
        
        public Guid? ProductId { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? LastMessageAt { get; set; }
        
        // Navigation properties
        [ForeignKey("InitiatorId")]
        public virtual User Initiator { get; set; } = null!;
        
        [ForeignKey("ReceiverId")]
        public virtual User Receiver { get; set; } = null!;
        
        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }
        
        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}
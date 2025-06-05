using System.ComponentModel.DataAnnotations;

namespace olx_be_api.Models
{
    public class Favorite
    {
        public int Id { get; set; }
        [Required]
        public Guid UserId { get; set; }
        public User User { get; set; }

        [Required]
        public long ProductId { get; set; }
        public Product Product { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace olx_be_api.Models
{
    public class Product
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public int Price { get; set; }
        public int? CategoryId { get; set; }
        public Guid UserId { get; set; }
        public bool IsSold { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Category? Category { get; set; }
        public User User { get; set; } = null!;
        public Location Location { get; set; } = new Location();
        public ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
    }
}
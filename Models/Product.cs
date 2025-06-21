using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace olx_be_api.Models
{
    public class Product
    {
        public long Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public int Price { get; set; }
        public int? CategoryId { get; set; }
        public bool IsSold { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiredAt { get; set; }
        public bool IsActive { get; set; } = true;

        public Category? Category { get; set; }
        public User User { get; set; } = null!;
        public Guid UserId { get; set; }
        public Guid? LocationId { get; set; }
        public Location Location { get; set; } = null!;
        public ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
        public ICollection<Favorite> FavoritedBy { get; set; } = new List<Favorite>();
        public ICollection<ActiveProductFeature> ActiveFeatures { get; set; } = new List<ActiveProductFeature>();
    }
}
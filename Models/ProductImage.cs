using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace olx_be_api.Models
{
    public class ProductImage
    {
        public Guid Id { get; set; }
        public long ProductId { get; set; }
        public string ImageUrl { get; set; } = null!;
        public bool IsCover { get; set; }

        public Product Product { get; set; } = null!;
    }
}
using System.ComponentModel.DataAnnotations;

namespace olx_be_api.DTO
{
    public class CreateProductDto
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }
        
        [Required]
        public Guid CategoryId { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Location { get; set; } = string.Empty;
        
        public List<string>? ImageUrls { get; set; }
    }

    public class UpdateProductDto
    {
        [MaxLength(200)]
        public string? Title { get; set; }
        
        [MaxLength(2000)]
        public string? Description { get; set; }
        
        [Range(0.01, double.MaxValue)]
        public decimal? Price { get; set; }
        
        public Guid? CategoryId { get; set; }
        
        [MaxLength(100)]
        public string? Location { get; set; }
        
        public bool? IsAvailable { get; set; }
    }

    public class ProductResponseDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public Guid SellerId { get; set; }
        public string SellerName { get; set; } = string.Empty;
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
        public int ViewCount { get; set; }
        public List<ProductImageResponseDto> Images { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
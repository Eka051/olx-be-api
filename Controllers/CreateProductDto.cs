using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace olx_be_api.DTO
{
    public class CreateProductDto
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = null!;
        
        [Required]
        [MaxLength(2000)]
        public string Description { get; set; } = null!;
        
        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }
        
        [Required]
        public Guid CategoryId { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Location { get; set; } = null!;
        
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
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public decimal Price { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = null!;
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = null!;
        public string Location { get; set; } = null!;
        public bool IsAvailable { get; set; }
        public int ViewCount { get; set; }
        public List<ProductImageResponseDto> Images { get; set; } = new List<ProductImageResponseDto>();
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
using System;
using System.ComponentModel.DataAnnotations;

namespace olx_be_api.DTO
{
    public class CreateProductImageDto
    {
        [Required]
        public Guid ProductId { get; set; }
        
        [Required]
        public string ImageUrl { get; set; } = null!;
        
        public bool IsMain { get; set; } = false;
    }

    public class UpdateProductImageDto
    {
        public bool? IsMain { get; set; }
    }

    public class ProductImageResponseDto
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string ImageUrl { get; set; } = null!;
        public bool IsMain { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
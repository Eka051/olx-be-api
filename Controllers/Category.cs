using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace olx_be_api.Models
{
    /// <summary>
    /// Represents a product category
    /// </summary>
    public class Category
    {
        [Key]
        public Guid Id { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = null!;
        
        [MaxLength(500)]
        public string? Description { get; set; }
        
        public string? IconUrl { get; set; }
        
        // Navigation properties
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
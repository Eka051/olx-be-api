using System;
using System.ComponentModel.DataAnnotations;

namespace olx_be_api.DTO
{
    public class CartCreateDTO
    {
        [Required]
        public int AdPackageId { get; set; }
        
        [Required]
        public long ProductId { get; set; }
        
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; } = 1;
    }

    public class CartResponseDTO
    {
        public Guid Id { get; set; }
        public int AdPackageId { get; set; }
        public string AdPackageName { get; set; } = null!;
        public int Quantity { get; set; }
        public long ProductId { get; set; }
        public string ProductTitle { get; set; } = null!;
        public int Price { get; set; }
        public int AdPackagePrice { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = null!;
    }
}

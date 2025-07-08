using System.ComponentModel.DataAnnotations;

namespace olx_be_api.DTO
{
    public class PremiumPurchaseDTO
    {
        [Required]
        public int PackageId { get; set; }
    }
}
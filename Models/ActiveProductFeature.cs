using olx_be_api.Models.Enums;

namespace olx_be_api.Models
{
    public class ActiveProductFeature
    {
        public int Id { get; set; }
        public long ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public AdFeatureType FeatureType { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int RemainingQuantity { get; set; }
    }
}

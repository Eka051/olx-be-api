using olx_be_api.Models.Enums;

namespace olx_be_api.Models
{
    public class AdPackageFeature
    {
        public int Id { get; set; }
        public AdFeatureType FeatureType { get; set; }
        public int Quantity { get; set; }
        public int DurationDays { get; set; }

        public int AdPackageId { get; set; }
        public AdPackage AdPackage { get; set; } = null!;
    }
}

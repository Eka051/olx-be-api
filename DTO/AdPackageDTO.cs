using olx_be_api.Models;
using olx_be_api.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace olx_be_api.DTO
{    public class AdPackageFeatureDTO
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public AdFeatureType FeatureType { get; set; }
        public int Quantity { get; set; }
        public int DurationDays { get; set; }
    }
    public class AdPackageDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int Price { get; set; }
        public List<AdPackageFeatureDTO> Features { get; set; } = new List<AdPackageFeatureDTO>();
    }

    public class CreateAdPackageDTO
    {
        [Required]
        public string Name { get; set; } = null!;
        [Required]
        [Range(0, int.MaxValue)]
        public int Price { get; set; }
        public List<AdPackageFeatureDTO> Features { get; set; } = new List<AdPackageFeatureDTO>();
    }    public class UpdateAdPackageDTO
    {
        [Required]
        public string Name { get; set; } = null!;
        [Range(0, int.MaxValue)]
        public int Price { get; set; }
        public List<AdPackageFeatureDTO> Features { get; set; } = new List<AdPackageFeatureDTO>();
    }

    public class UpdatePriceAdPackageDTO
    {
        [Required]
        [Range(0, int.MaxValue)]
        public int Price { get; set; }
    }

    public class PurchaseAdPackageDTO
    {
        [Required]
        public int AdPackageId { get; set; }
    }
}

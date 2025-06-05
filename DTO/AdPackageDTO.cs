using olx_be_api.Models;
using System.ComponentModel.DataAnnotations;

namespace olx_be_api.DTO
{
    public class AdPackageDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public AdPackageType Type { get; set; }
        public int Price { get; set; }
        public int DurationDays { get; set; }
    }

    public class CreateAdPackageDTO
    {
        [Required]
        public string Name { get; set; } = null!;
        [Required]
        public AdPackageType Type { get; set; }
        [Range(0, int.MaxValue)]
        public int Price { get; set; }
        [Range(1, 366)]
        public int DurationDays { get; set; }
    }

    public class UpdateAdPackageDTO
    {
        [Required]
        public string Name { get; set; } = null!;
        [Required]
        public AdPackageType Type { get; set; }
        [Range(0, int.MaxValue)]
        public int Price { get; set; }
        [Range(1, 366)]
        public int DurationDays { get; set; }
    }

    public class  UpdatePriceAdPackageDTO
    {
        [Required]
        [Range(0, int.MaxValue)]
        public int Price { get; set; }
    }
}

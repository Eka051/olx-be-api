namespace olx_be_api.DTO
{
    public class PremiumPackageDTO
    {
    }

    public class PremiumPackageCreateDTO
    {
        public string Name { get; set; } = null!;
        public int Price { get; set; }
        public int DurationDays { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class PremiumPackageResponseDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int Price { get; set; }
        public int DurationDays { get; set; }
        public bool IsActive { get; set; }
    }
}

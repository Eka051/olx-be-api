namespace olx_be_api.DTO
{

    public class PremiumPackageCreateDTO
    {
        public string? Description { get; set; }
        public int Price { get; set; }
        public int DurationDays { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class PremiumPackageResponseDTO
    {
        public int Id { get; set; }
        public string? Description { get; set; }
        public int Price { get; set; }
        public int DurationDays { get; set; }
        public bool IsActive { get; set; }
    }
}

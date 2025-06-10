namespace olx_be_api.Models
{
    public class PremiumPackage
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int Price { get; set; }
        public int DurationDays { get; set; }
        public bool IsActive { get; set; } = true;
    }
}

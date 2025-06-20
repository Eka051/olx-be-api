namespace olx_be_api.Models
{
    public class AdPackage
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int Price { get; set; }
        public int DurationDays { get; set; }
        public ICollection<AdPackageFeature> Features { get; set; } = new List<AdPackageFeature>();
    }
}

namespace olx_be_api.Models
{
    public class City
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int ProvinceId { get; set; }
        
        public Province Province { get; set; } = null!;
        public ICollection<District> Districts { get; set; } = new List<District>();
    }
}

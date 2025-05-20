namespace olx_be_api.Models
{
    public class District
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int CityId { get; set; }
        public virtual City City { get; set; } = null!;
    }
}

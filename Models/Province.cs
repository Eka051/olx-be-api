namespace olx_be_api.Models
{
    public class Province
    {
        public int id { get; set; }
        public string name { get; set; } = null!;

        public ICollection<City> cities { get; set; } = new List<City>();
    }
}

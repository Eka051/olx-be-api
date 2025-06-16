using System.ComponentModel.DataAnnotations;

namespace olx_be_api.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public ICollection<Product> Products { get; set; }
    }
}
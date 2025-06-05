namespace olx_be_api.DTO
{
    public class CreateProductDTO
    {
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public int Price { get; set; }
        public int CategoryId { get; set; }
        public int LocationId { get; set; }
        public List<string> Images { get; set; } = new List<string>();
    }

    public class ProductResponseDTO
    {
        public long Id { get; set; }
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public int Price { get; set; }
        public int CategoryId { get; set; }
        public int LocationId { get; set; }
        public List<string> Images { get; set; } = new List<string>();
        public DateTime CreatedAt { get; set; }
        public bool IsSold { get; set; } = false;
    }

    public class UpdateProductDTO
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public int? Price { get; set; }
        public long? CategoryId { get; set; }
        public int? LocationId { get; set; }
        public List<string>? Images { get; set; }
    }
}

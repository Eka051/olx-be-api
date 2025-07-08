namespace olx_be_api.DTO
{
    public class CreateProductDTO
    {
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public int Price { get; set; }
        public int CategoryId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public List<IFormFile> Images { get; set; } = new List<IFormFile>();
    }

    public class ProductResponseDTO
    {
        public long Id { get; set; }
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public int Price { get; set; }
        public bool IsSold { get; set; } = false;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = null!;
        public string? SellerId { get; set; }
        public string? SellerName { get; set; }
        public List<string> Images { get; set; } = new List<string>();

        public int? ProvinceId { get; set; }
        public string? ProvinceName { get; set; }
        public int? CityId { get; set; }
        public string? CityName { get; set; }
        public int? DistrictId { get; set; }
        public string? DistrictName { get; set; }
    }

    public class UpdateProductDTO
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public int? Price { get; set; }
        public int? CategoryId { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public List<string>? UrlsToDelete { get; set; } = new List<string>();

        public List<IFormFile>? NewImages { get; set; } = new List<IFormFile>();
    }
}
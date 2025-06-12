namespace olx_be_api.DTO
{
    public class CartCreateDTO
    {
        public int AdPackageId { get; set; }
        public int Quantity { get; set; }
        public long ProductId { get; set; }
    }

    public class CartResponseDTO
    {
        public Guid Id { get; set; }
        public int AdPackageId { get; set; }
        public string AdPackageName { get; set; } = null!;
        public int Quantity { get; set; }
        public long ProductId { get; set; }
        public string ProductTitle { get; set; } = null!;
        public Guid UserId { get; set; }
        public string UserName { get; set; } = null!;
    }
}

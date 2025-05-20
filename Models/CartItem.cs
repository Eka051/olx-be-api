namespace olx_be_api.Models
{
    public class CartItem
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public int AdPackageId { get; set; }
        public int Quantity { get; set; }

        public User User { get; set; } = null!;
        public AdPackage AdPackage { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}

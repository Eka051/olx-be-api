using System.ComponentModel.DataAnnotations;

namespace olx_be_api.DTO
{
    public class CreateFavoriteDto
    {
        [Required]
        public Guid ProductId { get; set; }
    }

    public class FavoriteResponseDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid ProductId { get; set; }
        public string ProductTitle { get; set; } = string.Empty;
        public decimal ProductPrice { get; set; }
        public string? MainImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
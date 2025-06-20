using System.ComponentModel.DataAnnotations;

namespace olx_be_api.DTO
{
    public class CreateFavoriteDTO
    {
        [Required]
        public long ProductId { get; set; }
    }

    public class FavoriteResponseDTO
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public long ProductId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    
}

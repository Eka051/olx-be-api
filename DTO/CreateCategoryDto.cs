using System.ComponentModel.DataAnnotations;

namespace olx_be_api.DTO
{
    public class CreateCategoryDto
    {
        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;
    }

    public class UpdateCategoryDto
    {
        [MaxLength(50)]
        public string? Name { get; set; }
        
    }

    public class CategoryResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
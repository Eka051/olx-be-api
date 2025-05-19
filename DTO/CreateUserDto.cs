using System.ComponentModel.DataAnnotations;

namespace olx_be_api.DTO
{
    public class CreateUserDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;
        
        [MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;
        
        public string? ProfileImageUrl { get; set; }
    }

    public class UpdateUserDto
    {
        [MaxLength(100)]
        public string? Name { get; set; }
        
        [MaxLength(20)]
        public string? PhoneNumber { get; set; }
        
        public string? ProfileImageUrl { get; set; }
    }

    public class UserResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? ProfileImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
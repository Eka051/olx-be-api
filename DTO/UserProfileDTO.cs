using Microsoft.AspNetCore.Http;
using olx_be_api.Models;

namespace olx_be_api.DTO
{
    public class UserProfileDTO
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public ProfileType ProfileType { get; set; } = ProfileType.Regular;
        public DateTime CreatedAt { get; set; }
        public int TotalAds { get; set; }
    }

    public class UpdateProfileDTO
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public IFormFile? ProfilePicture { get; set; }
    }
}

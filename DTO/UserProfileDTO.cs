using Microsoft.AspNetCore.Http;

namespace olx_be_api.DTO
{
    public class UserProfileDTO
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public int TotalAds { get; set; }
    }

    public class UpdateProfileDTO
    {
        public string? Name { get; set; }
        public string? PhoneNumber { get; set; }
        public IFormFile? ProfilePicture { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace olx_be_api.Models
{
    public class EmailOtp
    {
        [Key]
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiredAt { get; set; }
        public bool IsUsed { get; set; } = false;

        public Guid UserId { get; set; }
        public User? User { get; set; }

    }
}

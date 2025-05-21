using olx_be_api.Models;
using System.ComponentModel.DataAnnotations;

namespace olx_be_api.DTO
{
    public class LoginResponseDTO
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Token { get; set; }
        public string? Error { get; set; }
        public User User { get; set; }
    }
}

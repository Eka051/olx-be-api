using olx_be_api.Models;

namespace olx_be_api.DTO
{
    public class UserPremiumStatusDTO
    {
        public bool IsPremium { get; set; }
        public ProfileType ProfileType { get; set; }
        public DateTime? PremiumUntil { get; set; }
    }
}
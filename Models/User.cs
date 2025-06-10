using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace olx_be_api.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public ProfileType ProfileType { get; set; } = ProfileType.Regular;
        public DateTime? PremiumUntil { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string AuthProvider { get; set; } = null!;
        public string ProviderUid { get; set; } = null!;
        public DateTime CreatedAt { get; set; }

        public ICollection<UserRole> UserRoles =  new List<UserRole>();
        public ICollection<Product> Products { get; set; } = new List<Product>();
        public ICollection<ChatRoom> BuyerChatRooms { get; set; } = new List<ChatRoom>();
        public ICollection<ChatRoom> SellerChatRooms { get; set; } = new List<ChatRoom>();
        public ICollection<Message> SentMessages { get; set; } = new List<Message>();
        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    }

    public enum ProfileType
    {
        Regular,
        Premium
    }

}

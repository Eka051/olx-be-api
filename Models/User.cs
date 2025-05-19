using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace olx_be_api.Models
{

    public class User
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public string PasswordHash { get; set; } = string.Empty;
        
        [MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;
        
        public string? ProfileImageUrl { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
        public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
        public virtual ICollection<Transaction> PurchasedTransactions { get; set; } = new List<Transaction>();
        public virtual ICollection<Transaction> SoldTransactions { get; set; } = new List<Transaction>();
        public virtual ICollection<Chat> InitiatedChats { get; set; } = new List<Chat>();
        public virtual ICollection<Chat> ReceivedChats { get; set; } = new List<Chat>();
        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}

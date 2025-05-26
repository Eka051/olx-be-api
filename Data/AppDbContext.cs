using Microsoft.EntityFrameworkCore;
using olx_be_api.Helpers;
using olx_be_api.Models;

namespace olx_be_api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // User, Email and Roles
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<EmailOtp> EmailOtps { get; set; }

        // Product & Category
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }

        // Promotions
        public DbSet<AdPackage> AdPackages { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<AdTransaction> AdTransactions { get; set; }

        // Chat
        public DbSet<ChatRoom> ChatRooms { get; set; }
        public DbSet<Message> Messages { get; set; }

        // Locations
        public DbSet<Province> Provinces { get; set; }
        public DbSet<City> Cities { get; set; }
        public DbSet<District> Districts { get; set; }
        public DbSet<Location> Locations { get; set; }

        // Notifications
        public DbSet<Notification> Notifications { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User unique constraints
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Email).IsUnique();
                entity.HasIndex(u => u.PhoneNumber).IsUnique();
                entity.HasIndex(u => new { u.ProviderUid, u.AuthProvider }).IsUnique();
            });

            // Email
            modelBuilder.Entity<EmailOtp>().HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Product & ProductImage
            modelBuilder.Entity<Product>()
                .HasMany(p => p.ProductImages)
                .WithOne(pi => pi.Product)
                .HasForeignKey(pi => pi.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Product & User (Seller)
            modelBuilder.Entity<Product>()
                .HasOne(p => p.User)
                .WithMany(u => u.Products)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Product & Category
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany()
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            // Product & Location (Owned Entity)
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Location)
                .WithMany(l => l.Products)
                .HasForeignKey(p => p.LocationId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ProductImage>()
                .Property(pi => pi.ImageUrl)
                .IsRequired();

            modelBuilder.Entity<ChatRoom>()
                .HasOne(cr => cr.Buyer)
                .WithMany(u => u.BuyerChatRooms)
                .HasForeignKey(cr => cr.BuyerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ChatRoom>()
                .HasOne(cr => cr.Seller)
                .WithMany(u => u.SellerChatRooms)
                .HasForeignKey(cr => cr.SellerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany(u => u.SentMessages)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.ChatRoom)
                .WithMany(cr => cr.Messages)
                .HasForeignKey(m => m.ChatRoomId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Notification>()
                .Property(n => n.Message)
                .IsRequired();

            modelBuilder.Entity<Notification>()
                .Property(n => n.IsRead);

            modelBuilder.Entity<Notification>()
                .Property(n => n.CreatedAt);

            // CartItem & Product
            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Product)
                .WithMany()
                .HasForeignKey("ProductId")
                .OnDelete(DeleteBehavior.Cascade);

            // CartItem & User
            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.User)
                .WithMany(u => u.CartItems)
                .HasForeignKey(ci => ci.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // User & Role
            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.HasKey(ur => new { ur.UserId, ur.RoleId });

                entity.HasOne(ur => ur.User)
                    .WithMany(u => u.UserRoles)
                    .HasForeignKey(ur => ur.UserId);

                entity.HasOne(ur => ur.Role)
                    .WithMany(r => r.UserRoles)
                    .HasForeignKey(ur => ur.RoleId);
            });

            modelBuilder.Entity<AdPackage>()
                .Property(p => p.Type)
                .HasConversion<string>();

            modelBuilder.Entity<User>()
                .Property(u => u.ProfileType)
                .HasConversion<string>();

            modelBuilder.Entity<AdTransaction>()
               .Property(t => t.Status)
               .HasConversion<string>();

            modelBuilder.UseSnakeCase();
        }

    }

}
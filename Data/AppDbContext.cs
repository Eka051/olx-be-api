using Microsoft.EntityFrameworkCore;
using olx_be_api.Helpers;
using olx_be_api.Models;

namespace olx_be_api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<EmailOtp> EmailOtps { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ActiveProductFeature> ActiveProductFeatures { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<AdPackage> AdPackages { get; set; }
        public DbSet<AdPackageFeature> AdPackageFeatures { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<PremiumPackage> PremiumPackages { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<ChatRoom> ChatRooms { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Province> Provinces { get; set; }
        public DbSet<City> Cities { get; set; }
        public DbSet<District> Districts { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Favorite> Favorites { get; set; }
        public DbSet<TransactionItemDetail> TransactionItemDetails { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Email).IsUnique();
                entity.HasIndex(u => u.PhoneNumber).IsUnique();
                entity.HasIndex(u => new { u.ProviderUid, u.AuthProvider }).IsUnique();
                entity.Property(u => u.ProfileType).HasConversion<string>();
            });

            modelBuilder.Entity<EmailOtp>(entity =>
            {
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Product>(entity =>
            {
                entity.Property(p => p.Id).ValueGeneratedNever();
                entity.Property(p => p.IsActive).HasDefaultValue(true);

                entity.HasMany(p => p.ProductImages)
                    .WithOne(pi => pi.Product)
                    .HasForeignKey(pi => pi.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(p => p.User)
                    .WithMany(u => u.Products)
                    .HasForeignKey(p => p.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(p => p.Category)
                    .WithMany(c => c.Products)
                    .HasForeignKey(p => p.CategoryId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(p => p.Location)
                    .WithMany(l => l.Products)
                    .HasForeignKey(p => p.LocationId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasMany(p => p.FavoritedBy)
                    .WithOne(f => f.Product)
                    .HasForeignKey(f => f.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasMany(p => p.ActiveFeatures)
                       .WithOne(af => af.Product)
                       .HasForeignKey(af => af.ProductId);
            });

            modelBuilder.Entity<ProductImage>(entity =>
            {
                entity.Property(pi => pi.ImageUrl).IsRequired();
                entity.HasOne(pi => pi.Product)
                    .WithMany(p => p.ProductImages)
                    .HasForeignKey(pi => pi.ProductId)
                    .HasPrincipalKey(p => p.Id)
                    .OnDelete(DeleteBehavior.Cascade);

            });

            modelBuilder.Entity<ActiveProductFeature>(entity =>
            {
                entity.HasOne(af => af.Product)
                      .WithMany(p => p.ActiveFeatures)
                      .HasForeignKey(af => af.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.Property(af => af.FeatureType)
                      .HasConversion<string>();
            });

            modelBuilder.Entity<ChatRoom>(entity => {
                entity.HasOne(cr => cr.Buyer)
                    .WithMany(u => u.BuyerChatRooms)
                    .HasForeignKey(cr => cr.BuyerId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(cr => cr.Seller)
                    .WithMany(u => u.SellerChatRooms)
                    .HasForeignKey(cr => cr.SellerId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(cr => cr.Product)
                    .WithMany()
                    .HasForeignKey(cr => cr.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasOne(m => m.Sender)
                    .WithMany(u => u.SentMessages)
                    .HasForeignKey(m => m.SenderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(m => m.ChatRoom)
                    .WithMany(cr => cr.Messages)
                    .HasForeignKey(m => m.ChatRoomId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasOne(n => n.User)
                    .WithMany(u => u.Notifications)
                    .HasForeignKey(n => n.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.Property(n => n.Message).IsRequired();
            });

            modelBuilder.Entity<CartItem>(entity =>
            {
                entity.HasOne(ci => ci.User)
                    .WithMany(u => u.CartItems)
                    .HasForeignKey(ci => ci.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ci => ci.AdPackage)
                    .WithMany()
                    .HasForeignKey(ci => ci.AdPackageId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Transaction>(entity => {
                entity.HasIndex(t => t.InvoiceNumber).IsUnique();

                entity.HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

                entity.Property(t => t.Status).HasConversion<string>();
                entity.Property(t => t.Type).HasConversion<string>();
            });

            modelBuilder.Entity<TransactionItemDetail>(entity =>
            {
                entity.HasKey(tid => new { tid.AdPackageId, tid.ProductId });
            });

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

            modelBuilder.Entity<Favorite>(entity =>
            {
                entity.HasIndex(f => new { f.UserId, f.ProductId }).IsUnique();

                entity.HasOne(f => f.User)
                    .WithMany(u => u.Favorites)
                    .HasForeignKey(f => f.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(f => f.Product)
                    .WithMany(p => p.FavoritedBy)
                    .HasForeignKey(f => f.ProductId)
                    .HasPrincipalKey(p => p.Id)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<AdPackage>(entity =>
            {
                entity.HasMany(p => p.Features)
                    .WithOne(f => f.AdPackage)
                    .HasForeignKey(f => f.AdPackageId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<AdPackageFeature>(entity => 
                entity.Property(f => f.FeatureType)
                .HasConversion<string>());

            modelBuilder.UseSnakeCase();
        }
    }
}

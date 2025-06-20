using Microsoft.EntityFrameworkCore;
using olx_be_api.Models;
using System;
using System.Linq;

namespace olx_be_api.Data
{
    public static class DataSeeder
    {        public static void SeedDatabase(AppDbContext context)
        {
            context.Database.Migrate();
            SeedRole(context);
            SeedAdmin(context);
            // Location data will be created dynamically when products are added
        }

        private static void SeedRole(AppDbContext context)
        {
            bool hasChanges = false;

            if (!context.Roles.Any(r => r.Name == "Admin"))
            {
                context.Roles.Add(new Role { Id = 1, Name = "Admin" });
                Console.WriteLine("Seeding Role.... Admin");
                hasChanges = true;
            }

            if (!context.Roles.Any(r => r.Name == "User"))
            {
                context.Roles.Add(new Role { Id = 2, Name = "User" });
                Console.WriteLine("Seeding Role.... User");
                hasChanges = true;
            }

            if (hasChanges)
            {
                context.SaveChanges();
            }
        }

        private static void SeedAdmin(AppDbContext context)
        {
            string adminEmail = "olxclonenoreply@gmail.com";
            string adminName = "Admin / Developer OLX Clone";

            var adminUser = context.Users
                .FirstOrDefault(u => u.Email == adminEmail && u.AuthProvider == "email");

            bool isAdminNew = false;
            if (adminUser == null)
            {
                adminUser = new User
                {
                    Id = Guid.NewGuid(),
                    Name = adminName,
                    Email = adminEmail,
                    AuthProvider = "email",
                    ProviderUid = Guid.NewGuid().ToString(),
                    ProfileType = ProfileType.Regular,
                    CreatedAt = DateTime.UtcNow
                };
                context.Users.Add(adminUser);
                context.SaveChanges();
                isAdminNew = true;
                Console.WriteLine($"Seeding Admin User.... {adminName} ({adminEmail})");
            }
            else
            {
                Console.WriteLine($"Admin User {adminEmail} already exists.");
            }

            var adminRole = context.Roles.FirstOrDefault(r => r.Name == "Admin");
            if (adminRole == null)
            {
                Console.WriteLine("ERROR: Role 'Admin' tidak ditemukan. Pastikan SeedRole berjalan dengan benar dan menyimpan perubahan.");
                return;
            }

            var existingUserRole = context.UserRoles
                                        .FirstOrDefault(ur => ur.UserId == adminUser.Id && ur.RoleId == adminRole.Id);

            if (existingUserRole == null)
            {
                context.UserRoles.Add(new UserRole
                {
                    UserId = adminUser.Id,
                    RoleId = adminRole.Id
                });
                context.SaveChanges();
                Console.WriteLine($"Assigned 'Admin' role to {adminEmail}.");
            }
            else
            {
                if (!isAdminNew)
                {
                    Console.WriteLine($"{adminEmail} already has 'Admin' role.");
                }
            }        }
    }
}
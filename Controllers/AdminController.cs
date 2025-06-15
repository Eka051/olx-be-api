using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using olx_be_api.Data;
using olx_be_api.DTO;
using olx_be_api.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace olx_be_api.Controllers
{
    [Route("api/admin")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("stats")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDashboardStats()
        {
            var totalUsers = await _context.Users.CountAsync(u => !u.UserRoles.Any(r => r.Role.Name == "Admin"));
            var activeProducts = await _context.Products.CountAsync(p => !p.IsSold);
            var totalCategories = await _context.Categories.CountAsync();
            var totalRevenue = await _context.Transactions
                                       .Where(t => t.Status == Models.TransactionStatus.Success)
                                       .SumAsync(t => (long)t.Amount);

            var stats = new
            {
                totalUsers,
                activeProducts,
                totalCategories,
                totalRevenue
            };

            return Ok(new ApiResponse<object> { success = true, data = stats });
        }

        [HttpGet("users")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<List<UserProfileDTO>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users
                .Where(u => !u.UserRoles.Any(r => r.Role.Name == "Admin"))
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new UserProfileDTO
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    ProfilePictureUrl = u.ProfilePictureUrl,
                    CreatedAt = u.CreatedAt,
                    TotalAds = _context.Products.Count(p => p.UserId == u.Id)
                })
                .ToListAsync();

            return Ok(new ApiResponse<List<UserProfileDTO>> { success = true, data = users });
        }

        [HttpGet("growth-chart")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetGrowthChartData()
        {
            var sixMonthsAgo = DateTime.UtcNow.AddMonths(-5).Date;
            var startDate = new DateTime(sixMonthsAgo.Year, sixMonthsAgo.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var monthlyUsers = await _context.Users
                .Where(u => u.CreatedAt >= startDate && !u.UserRoles.Any(r => r.Role.Name == "Admin"))
                .GroupBy(u => new { u.CreatedAt.Year, u.CreatedAt.Month })
                .Select(g => new { Year = g.Key.Year, Month = g.Key.Month, Count = g.Count() })
                .ToListAsync();

            var monthlyRevenue = await _context.Transactions
                .Where(t => t.Status == Models.TransactionStatus.Success && t.PaidAt.HasValue && t.PaidAt.Value >= startDate)
                .GroupBy(t => new { t.PaidAt!.Value.Year, t.PaidAt.Value.Month })
                .Select(g => new { Year = g.Key.Year, Month = g.Key.Month, Total = g.Sum(t => t.Amount) })
                .ToListAsync();

            var labels = new List<string>();
            var usersData = new List<int>();
            var revenueData = new List<long>();

            for (int i = 5; i >= 0; i--)
            {
                var month = DateTime.UtcNow.AddMonths(-i);
                labels.Add(month.ToString("MMM", new CultureInfo("id-ID")));

                var userStat = monthlyUsers.FirstOrDefault(s => s.Year == month.Year && s.Month == month.Month);
                usersData.Add(userStat?.Count ?? 0);

                var revenueStat = monthlyRevenue.FirstOrDefault(s => s.Year == month.Year && s.Month == month.Month);
                revenueData.Add(revenueStat?.Total ?? 0);
            }

            var chartData = new { labels, usersData, revenueData };
            return Ok(new ApiResponse<object> { success = true, data = chartData });
        }
    }
}

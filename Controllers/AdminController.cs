using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using olx_be_api.Data;
using olx_be_api.DTO;
using olx_be_api.Helpers;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace olx_be_api.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("dashboard/stats")]
        [ProducesResponseType(typeof(ApiResponse<DashboardStatsDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var totalUsers = await _context.Users.CountAsync(u => !u.UserRoles.Any(r => r.Role.Name == "Admin"));
                var activeProducts = await _context.Products.CountAsync(p => !p.IsSold);
                var totalCategories = await _context.Categories.CountAsync();
                var totalRevenue = await _context.Transactions
                                           .Where(t => t.Status == Models.TransactionStatus.Success)
                                           .SumAsync(t => (long)t.Amount);

                var stats = new DashboardStatsDTO
                {
                    TotalUsers = totalUsers,
                    ActiveProducts = activeProducts,
                    TotalCategories = totalCategories,
                    TotalRevenue = totalRevenue
                };

                return Ok(new ApiResponse<DashboardStatsDTO>
                {
                    success = true,
                    message = "Berhasil mengambil statistik dashboard",
                    data = stats
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ApiErrorResponse
                    {
                        success = false,
                        message = "Terjadi kesalahan saat mengambil statistik dashboard",
                        errors = new { error = ex.Message }
                    });
            }
        }

        [HttpGet("users")]
        [ProducesResponseType(typeof(ApiResponse<List<UserProfileDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllUsers()
        {
            try
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
                        TotalAds = u.Products.Count()
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<List<UserProfileDTO>>
                {
                    success = true,
                    message = "Berhasil mengambil data semua pengguna",
                    data = users
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ApiErrorResponse
                    {
                        success = false,
                        message = "Terjadi kesalahan saat mengambil data pengguna",
                        errors = new { error = ex.Message }
                    });
            }
        }

        [HttpGet("dashboard/growth-chart")]
        [ProducesResponseType(typeof(ApiResponse<GrowthChartDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetGrowthChartData()
        {
            try
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

                var chartData = new GrowthChartDTO
                {
                    Labels = labels,
                    UsersData = usersData,
                    RevenueData = revenueData
                };

                return Ok(new ApiResponse<GrowthChartDTO>
                {
                    success = true,
                    message = "Berhasil mengambil data grafik pertumbuhan",
                    data = chartData
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ApiErrorResponse
                    {
                        success = false,
                        message = "Terjadi kesalahan saat mengambil data grafik pertumbuhan",
                        errors = new { error = ex.Message }
                    });
            }
        }
    }
}
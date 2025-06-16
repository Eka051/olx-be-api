namespace olx_be_api.DTO
{

    public class DashboardStatsDTO
    {
        public int TotalUsers { get; set; }
        public int ActiveProducts { get; set; }
        public int TotalCategories { get; set; }
        public long TotalRevenue { get; set; }
    }

    public class GrowthChartDTO
    {
        public List<string> Labels { get; set; } = new();
        public List<int> UsersData { get; set; } = new();
        public List<long> RevenueData { get; set; } = new();
    }
}

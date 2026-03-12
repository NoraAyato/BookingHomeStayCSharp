using DoAnCs.Areas.Host.ModelsView;

namespace DoAnCs.Areas.Admin.ModelsView
{
    public class DashboardStatsData
    {
        public decimal TotalRevenue { get; set; }
        public decimal TotalProfit { get; set; } // Thêm tổng lợi nhuận
        public int InvoiceCount { get; set; }
        public int PaidInvoiceCount { get; set; }
        public int UnpaidInvoiceCount { get; set; }
        public int BookingCount { get; set; }
        public int ConfirmedBookingCount { get; set; }
        public int PendingBookingCount { get; set; }
        public int UserCount { get; set; }
        public int ActiveUserCount { get; set; }
        public int InactiveUserCount { get; set; }
        public int HomestayCount { get; set; }
        public int ActiveHomestayCount { get; set; }
        public int NewsCount { get; set; }
        public int ServiceCount { get; set; }
        public List<MonthlyRevenue> MonthlyRevenue { get; set; }
        public List<MonthlyProfit> MonthlyProfit { get; set; } // Thêm lợi nhuận theo tháng
        public List<MonthlyBooking> MonthlyBookings { get; set; }
        public List<PopularHomestay> PopularHomestays { get; set; }
        public List<PopularArea> PopularAreas { get; set; }
        public List<PopularNews> PopularNews { get; set; }
        public List<PopularService> PopularServices { get; set; }
    }
}

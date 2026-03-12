namespace DoAnCs.Areas.Host.ModelsView
{
    public class HostDashboardStatsData
    {
        public decimal TotalRevenue { get; set; }
        public decimal TotalProfit { get; set; }
        public int BookingCount { get; set; }
        public int ConfirmedBookingCount { get; set; }
        public int PendingBookingCount { get; set; }
        public int RoomCount { get; set; }
        public int AvailableRoomCount { get; set; }
        public double OccupancyRate { get; set; }
        public List<MonthlyRevenue> MonthlyRevenue { get; set; } = new List<MonthlyRevenue>();
        public List<MonthlyBooking> MonthlyBookings { get; set; } = new List<MonthlyBooking>();
        public List<HomestayService> HomestayServices { get; set; } = new List<HomestayService>();
    }
}

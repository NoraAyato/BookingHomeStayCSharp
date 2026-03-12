namespace DoAnCs.Areas.Host.ModelsView
{
    public class HostRecentActivitiesData
    {
        public List<RecentBooking> RecentBookings { get; set; } = new List<RecentBooking>();
        public List<RecentInvoice> RecentInvoices { get; set; } = new List<RecentInvoice>();
        public List<RecentActivity> RecentActivities { get; set; } = new List<RecentActivity>();
    }
}

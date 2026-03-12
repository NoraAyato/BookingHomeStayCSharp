namespace DoAnCs.Areas.Admin.ModelsView
{
    public class RecentInvoice
    {
        public string Id { get; set; }
        public string CustomerName { get; set; }
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
    }
}

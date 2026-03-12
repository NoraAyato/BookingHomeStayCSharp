namespace DoAnCs.Areas.Admin.ModelsView
{
    public class PopularHomestay
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Image { get; set; }
        public int BookingCount { get; set; }
        public decimal? OriginalPrice { get; set; }
        public decimal? Rank { get; internal set; }
        public string Address { get; internal set; }
    }
}

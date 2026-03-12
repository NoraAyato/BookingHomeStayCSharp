namespace DoAnCs.Models.ViewModels
{
    public class HomestayCardView
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Image { get; set; }
        public string Address { get; set; }
        public double Rating { get; set; }
        public int? DiscountPercent { get; set; }
        public decimal? OriginalPrice { get; set; }
        public decimal Price { get; set; }
    }
}

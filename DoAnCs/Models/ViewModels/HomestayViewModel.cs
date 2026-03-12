namespace DoAnCs.Models.ViewModels
{
    public class HomestayViewModel
    {
        public IEnumerable<Homestay> Homestays { get; set; }
        public IEnumerable<KhuVuc> KhuVucs { get; set; }
        public string SearchString { get; set; }
        public string LocationFilter { get; set; }
        public decimal? PriceFilter { get; set; }
        public decimal? RatingFilter { get; set; }
        public string SortOrder { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}

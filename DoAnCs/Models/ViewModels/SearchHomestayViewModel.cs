using System.ComponentModel.DataAnnotations;

namespace DoAnCs.Models.ViewModels
{
    public class SearchHomestayViewModel
    {
        public List<HomestaySearchResultViewModel> Homestays { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string Location { get; set; }
        public string CheckInDate { get; set; }
        public string CheckOutDate { get; set; }
        public string SortOrder { get; set; }
        public string ErrorMessage { get; set; }
    }
}

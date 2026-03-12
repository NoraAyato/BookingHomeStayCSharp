namespace DoAnCs.Models.ViewModels
{
    public class HomestayDetailsViewModel
    {
        public Homestay Homestay { get; set; }
        public List<PhongDetailsViewModel> AvailableRooms { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int NumberOfNights { get; set; }
        public string HostEmail { get; set; }
        public string HostPhone { get; set; }
        public List<DichVu> DichVus { get; set; }
        public List<DanhGia> DanhGias { get; set; }
    }
}

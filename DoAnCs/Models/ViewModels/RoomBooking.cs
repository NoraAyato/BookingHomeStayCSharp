namespace DoAnCs.Models.ViewModels
{
    public class RoomBooking
    {
        public string Ma_Phong { get; set; }
        public string TenPhong { get; set; }
        public string Ten_Homestay { get; set; }
        public decimal DonGia { get; set; }
        public int SoNguoi { get; set; }
        public decimal RoomTotal { get; set; }
        public string Ma_Phieu { get; set; } // Ma_Phieu cho PhieuSuDungDV
        public List<ServiceBooking> Services { get; set; } = new List<ServiceBooking>();
    }
}

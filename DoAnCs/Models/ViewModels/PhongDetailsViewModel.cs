namespace DoAnCs.Models.ViewModels
{
    public class PhongDetailsViewModel
    {
        public string Ma_Phong { get; set; }
        public string TenPhong { get; set; }
        public decimal DonGia { get; set; }
        public int SoNguoi { get; set; }
        public string TenLoai { get; set; }
        public List<ChiTietPhong> TienNghis { get; set; }
        public List<HinhAnhPhong> HinhAnhs { get; set; }
        public decimal TotalPhuThu { get; set; }
    }
}

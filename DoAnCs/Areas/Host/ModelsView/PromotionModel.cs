namespace DoAnCs.Areas.Host.ModelsView
{
    public class PromotionModel
    {
        public string Ma_KM { get; set; }
        public string NoiDung { get; set; }
        public DateTime NgayBatDau { get; set; }
        public DateTime HSD { get; set; }
        public float ChietKhau { get; set; }
        public string LoaiChietKhau { get; set; }
        public int? SoDemToiThieu { get; set; }
        public int? SoNgayDatTruoc { get; set; }
        public bool ChiApDungChoKhachMoi { get; set; }
        public bool ApDungChoTatCaPhong { get; set; }
        public string TrangThai { get; set; }
        public int SoLuong { get; set; }
        public List<string> Phongs { get; set; }
    }
}

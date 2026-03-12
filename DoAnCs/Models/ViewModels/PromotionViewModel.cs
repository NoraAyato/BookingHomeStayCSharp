namespace DoAnCs.Models.ViewModels
{
    public class PromotionViewModel
    {
        public string MaKM { get; set; }
        public string NoiDung { get; set; }
        public decimal ChietKhau { get; set; }
        public string LoaiChietKhau { get; set; } // "Percentage" hoặc "Fixed"
        public string HSD { get; set; } // Định dạng "dd/MM/yyyy"
        public string DieuKien { get; set; } // Chuỗi điều kiện áp dụng
        public int SoLuongConLai { get; set; }
    }
}

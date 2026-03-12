namespace DoAnCs.Models.ViewModels
{
    public class KhuyenMaiViewModel
    {
        public string MaKM { get; set; }
        public string TieuDe { get; set; }              // Tiêu đề khuyến mãi
        public string MoTa { get; set; }                // Mô tả ngắn gọn
        public string MaGiamGia { get; set; }           // Mã khuyến mãi hiển thị
        public DateTime HSD { get; set; }
        public string AnhDaiDien { get; set; }          // (Tuỳ chọn) ảnh đại diện
        public string ThoiGianConLai { get; set; }      // Chuỗi hiển thị ví dụ: "Còn 5 ngày"
    }
}

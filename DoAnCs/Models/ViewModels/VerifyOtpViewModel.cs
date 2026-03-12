using System.ComponentModel.DataAnnotations;

namespace DoAnCs.Models.ViewModels
{
    public class VerifyOtpViewModel
    {
        public string Email { get; set; }
        public string Otp { get; set; }
    }
    public class AddReviewViewModel
    {
        public string MaPDPhong { get; set; } = null!;
        public string? BinhLuan { get; set; }
        public short Rating { get; set; }
        public IFormFile? HinhAnh { get; set; }
    }
    public class CancelBookingViewModel
    {
        [Required]
        public string MaPDPhong { get; set; }
        public string? LyDo { get; set; }
        public string? TenNganHang { get; set; }
        public string? SoTaiKhoan { get; set; }
    }
    public class AddServiceViewModel
    {
        public string MaPDPhong { get; set; }
        public List<RoomServiceItem> Rooms { get; set; }
    }

    public class RoomServiceItem
    {
        public string MaPhong { get; set; }
        public List<ServiceItem> Services { get; set; }
    }

    public class ServiceItem
    {
        public string MaDv { get; set; }
        public decimal SoLuong { get; set; }
    }
    public class ApplyPromotionViewModel
    {
        public string PromotionId { get; set; }
    }
}

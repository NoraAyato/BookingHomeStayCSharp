using System.ComponentModel.DataAnnotations;

namespace DoAnCs.Models.ViewModels
{
    public class RegisterHomestayViewModel
    {
        [Required(ErrorMessage = "Tên homestay không được để trống")]
        [StringLength(100, ErrorMessage = "Tên homestay không được vượt quá 100 ký tự")]
        public string Ten_Homestay { get; set; }

        [Required(ErrorMessage = "Địa chỉ không được để trống")]
        [StringLength(200, ErrorMessage = "Địa chỉ không được vượt quá 200 ký tự")]
        public string DiaChi { get; set; }

        [Required(ErrorMessage = "Giá mỗi đêm không được để trống")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Giá mỗi đêm phải lớn hơn 0")]
        public decimal PricePerNight { get; set; }

        [Required(ErrorMessage = "Mã khu vực không được để trống")]
        public string Ma_KV { get; set; }

        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
        public string? MoTa { get; set; }

        public IFormFile? HinhAnh { get; set; } // File upload cho hình ảnh
        public decimal Hang { get; internal set; }
    }
}

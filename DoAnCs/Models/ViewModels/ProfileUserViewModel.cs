using System.ComponentModel.DataAnnotations;

namespace DoAnCs.Models.ViewModels
{
    // DTO cho cập nhật hồ sơ
    public class ProfileUserUpdateModel
    {
        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự")]
        public string FullName { get; set; }

        [StringLength(200, ErrorMessage = "Địa chỉ không được vượt quá 200 ký tự")]
        public string? Address { get; set; }

        [Display(Name = "Số điện thoại")]
        public string? PhoneNumber { get; set; }

        public DateTime? DateOfBirth { get; set; }
    }

    // ViewModel để hiển thị thông tin
    public class ProfileUserViewModel
    {
        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự")]
        public string FullName { get; set; }

        [StringLength(200, ErrorMessage = "Địa chỉ không được vượt quá 200 ký tự")]
        public string? Address { get; set; }

        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public string? ProfilePicture { get; set; }

        [EmailAddress]
        public string Email { get; set; }

        public string TrangThai { get; set; }

        public decimal TichDiem { get; set; }
        public List<PhieuDatPhong> PhieuDatPhongs { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}
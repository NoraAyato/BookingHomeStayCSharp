using System.ComponentModel.DataAnnotations;

namespace DoAnCs.Models.ViewModels
{
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Mật khẩu cũ là bắt buộc.")]
        public string OldPassword { get; set; }

        [Required(ErrorMessage = "Mật khẩu mới là bắt buộc.")]
        [StringLength(100, ErrorMessage = "Mật khẩu mới phải có ít nhất {8} ký tự.", MinimumLength = 8)]
        public string NewPassword { get; set; }
    }
}

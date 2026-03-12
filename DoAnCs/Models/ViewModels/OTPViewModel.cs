using System.ComponentModel.DataAnnotations;

namespace DoAnCs.Models.ViewModels
{
    public class OTPViewModel
    {
        [Required(ErrorMessage = "Mã OTP là bắt buộc.")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Mã OTP phải là 6 chữ số.")]
        public string OTP { get; set; }
    }
}

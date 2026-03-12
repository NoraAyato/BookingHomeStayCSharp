using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnCs.Models
{
    public class PhieuHuyHopDong
    {
        [Key]
        public string Ma_PhieuHuy { get; set; }

        [Required(ErrorMessage = "Mã hợp đồng không được để trống")]
        [ForeignKey("HopDong")]
        public string Ma_HopDong { get; set; }

        [Required(ErrorMessage = "Người hủy không được để trống")]
        [StringLength(50)]
        public string NguoiHuy { get; set; } // ID của người hủy (ApplicationUser hoặc Admin)

        [Required(ErrorMessage = "Lý do hủy không được để trống")]
        [StringLength(200, ErrorMessage = "Lý do hủy không được vượt quá 200 ký tự")]
        public string LyDoHuy { get; set; }

        public DateTime NgayHuy { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string TrangThai { get; set; } = "Đã hủy"; // Có thể mở rộng: Đang xử lý, Đã hủy

        public virtual HopDong HopDong { get; set; }
    }
}
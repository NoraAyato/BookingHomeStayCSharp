using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnCs.Models
{
    public class HopDong
    {
        [Key]
        public string Ma_HopDong { get; set; }

        [Required(ErrorMessage = "Mã người dùng không được để trống")]
        [ForeignKey("ApplicationUser")]
        public string Ma_ND { get; set; }

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
        [ForeignKey("KhuVuc")]
        public string Ma_KV { get; set; }

        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
        public string? MoTa { get; set; }
        [Required(ErrorMessage = "Hạng của homestay không được để trống")]
        [Range(1, 5, ErrorMessage = "Hạng của homestay phải từ 1 đến 5 sao")]
        public decimal Hang { get; set; } 
        public string? HinhAnh { get; set; }

        [Required(ErrorMessage = "Trạng thái không được để trống")]
        [StringLength(50, ErrorMessage = "Trạng thái không được vượt quá 50 ký tự")]
        public string TrangThai { get; set; } = "Đang chờ duyệt"; // Đang chờ duyệt, Đã duyệt, Từ chối, Đã hủy

        public DateTime NgayGui { get; set; }

        public DateTime? NgayDuyet { get; set; }

        [StringLength(200)]
        public string? LyDoTuChoi { get; set; }

        // Navigation properties
        public virtual ApplicationUser ApplicationUser { get; set; }
        public virtual KhuVuc KhuVuc { get; set; }
        public virtual ICollection<PhieuHuyHopDong> PhieuHuyHopDongs { get; set; }
    }
}
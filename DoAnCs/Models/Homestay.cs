using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnCs.Models
{
    public class Homestay
    {
        [Key]
        public string ID_Homestay { get; set; }
        [Required(ErrorMessage = "Mã khu vực không được để trống")]
        [ForeignKey("KhuVuc")]
        public string Ma_KV { get; set; }
        [Required(ErrorMessage = "Tên homestay không được để trống")]
        [StringLength(100, ErrorMessage = "Tên homestay không được vượt quá 100 ký tự")]
        public string Ten_Homestay { get; set; }
        [Required(ErrorMessage = "Trạng thái không được để trống")]
        [StringLength(50, ErrorMessage = "Trạng thái không được vượt quá 50 ký tự")]
        public string TrangThai { get; set; }
        public decimal PricePerNight { get; set; }
        public string? HinhAnh { get; set; }
        [Required(ErrorMessage = "Mã người dùng không được để trống")]
        [ForeignKey("NguoiDung")]
        public string Ma_ND { get; set; }
        public string DiaChi { get; set; }
        public DateTime NgayTao { get; set; } = DateTime.Now;
        public decimal? Hang { get; set; } // Trường mới
        public virtual KhuVuc KhuVuc { get; set; }
        public virtual ApplicationUser NguoiDung { get; set; }
        public virtual ICollection<Phong> Phongs { get; set; }
        public virtual ICollection<DichVu> DichVus { get; set; }
        public virtual ICollection<DanhGia> DanhGias { get; set; }
        public virtual ICollection<ChinhSach> ChinhSachs { get; set; }
    
    }
}
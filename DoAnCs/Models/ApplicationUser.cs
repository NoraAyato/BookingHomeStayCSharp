using Microsoft.AspNetCore.Identity;

namespace DoAnCs.Models
{
    using Microsoft.AspNetCore.Identity;
    using System.ComponentModel.DataAnnotations;

    public class ApplicationUser : IdentityUser
    {
        [StringLength(100)]
        [Required]
        public string FullName { get; set; }

        [StringLength(200)]
        public string? Address { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public string? ProfilePicture { get; set; } // Thêm ảnh đại diện
        public bool IsRecieveEmail { get; set; } = false;
        public DateTime NgayTao { get; set; } = DateTime.Now;
        public decimal? tichdiem { get; set; }
        public string TrangThai { get; set; } = "Hoạt động"; // Hoạt động, Khóa, Đang chờ duyệt
        // 2FA fields
        public string? Encrypted2FASecretKey { get; set; } // Khóa bí mật đã mã hóa
        // Navigation properties
        public virtual ICollection<PhieuDatPhong> PhieuDatPhongs { get; set; }
        public virtual ICollection<Homestay> Homestays { get; set; }
        public virtual ICollection<DanhGia> DanhGias { get; set; }
        public virtual ICollection<HoaDon> HoaDons { get; set; }
        public virtual ICollection<BinhLuan> BinhLuans { get; set; }
        public virtual ICollection<KhuyenMai> KhuyenMais { get; set; } 
        public virtual ICollection<HopDong> HopDongs { get; set; }
    }
}

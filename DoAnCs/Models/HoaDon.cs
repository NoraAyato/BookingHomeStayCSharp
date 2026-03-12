using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnCs.Models
{
    public class HoaDon
    {
        [Key]
        [StringLength(20)]
        public string Ma_HD { get; set; }
        
        public decimal TongTien { get; set; }
        public DateTime NgayLap { get; set; }
        public decimal Thue { get; set; }
        public string TrangThai { get; set; }
        [ForeignKey("NguoiDung")]
        public string Ma_ND { get; set; }
        public virtual ApplicationUser NguoiDung { get; set; }

        public virtual ICollection<ApDungKM> ApDungKMs { get; set; }
        public virtual ICollection<ThanhToan> ThanhToans { get; set; }
        public  virtual ICollection<ChiTietHoaDon> ChiTietHoaDons { get; set; }
    }
}
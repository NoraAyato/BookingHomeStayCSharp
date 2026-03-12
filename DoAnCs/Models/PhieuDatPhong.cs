using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnCs.Models
{
    public class PhieuDatPhong
    {
        [Key]
        public string Ma_PDPhong { get; set; }

        [ForeignKey("NguoiDung")]
        public string Ma_ND { get; set; }
 
        public DateTime NgayLap { get; set; } = DateTime.Now;
        public string TrangThai { get; set; } // Đã xác nhận (tức là đã đặt , đã thanh toán thành công ), Chờ xác nhận (mới lên đơn , chưa thanh toán chưa đặt ), Đã hủy
        public virtual ApplicationUser NguoiDung { get; set; }
        public virtual ICollection<ChiTietDatPhong> ChiTietDatPhongs { get; set; }
        public virtual ICollection<ChiTietHoaDon> ChiTietHoaDons { get; set; }
        public virtual ICollection<PhieuHuyPhong> PhieuHuyPhongs { get; set; }

    }
}
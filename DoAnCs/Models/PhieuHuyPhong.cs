using DoAnCs.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnCs.Models
{
    public class PhieuHuyPhong
    {
        [Key]
        [StringLength(20)]
        public string MaPHP { get; set; }

        [ForeignKey(nameof(PhieuDatPhong))]
        public string Ma_PDPhong { get; set; }

        [StringLength(30)]
        public string LyDo { get; set; }
        public string ? TenNganHang { get; set; }
        public string ? SoTaiKhoan { get; set; }
        public DateTime NgayHuy { get; set; }
        public string? NguoiHuy { get; set; } 

        [StringLength(20)]
        public string TrangThai { get; set; }

        public virtual PhieuDatPhong PhieuDatPhong { get; set; }
    }
}

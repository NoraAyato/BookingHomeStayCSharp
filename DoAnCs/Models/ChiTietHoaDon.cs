using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnCs.Models
{
    [PrimaryKey(nameof(Ma_HD), nameof(Ma_PDPhong))]
    public class ChiTietHoaDon
    {
        [ForeignKey("HoaDon")]
        public string Ma_HD { get; set; }
        [ForeignKey("PhieuDatPhong")]

        public string Ma_PDPhong { get; set; }

        public virtual HoaDon HoaDon { get; set; }
        public virtual PhieuDatPhong PhieuDatPhong { get; set; }
    }
}

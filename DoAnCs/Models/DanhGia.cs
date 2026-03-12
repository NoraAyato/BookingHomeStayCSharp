using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnCs.Models
{
    [PrimaryKey(nameof(ID_DG))] 
    public class DanhGia
    {
        public string ID_DG { get; set; }
        [ForeignKey("NguoiDung")]
        public string Ma_ND { get; set; }
        [ForeignKey("Homestay")]
        public string ID_Homestay { get; set; }
        [ForeignKey("PhieuDatPhong")]
        public string Ma_PDPhong { get; set; }
        public string BinhLuan { get; set; }
        public DateTime NgayDanhGia { get; set; }
        public string? HinhAnh { get; set; }
        public short Rating { get; set; }

        public virtual ApplicationUser NguoiDung { get; set; }
        public virtual Homestay Homestay { get; set; }
        public virtual PhieuDatPhong PhieuDatPhong { get; set; } // Navigation property
    }
}
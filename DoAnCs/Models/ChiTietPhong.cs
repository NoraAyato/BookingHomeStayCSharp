using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DoAnCs.Models
{
    [PrimaryKey(nameof(Ma_Phong), nameof(Ma_TienNghi))]
    public class ChiTietPhong
    {
        [ForeignKey("Phong")]
        public string Ma_Phong { get; set; }

        [ForeignKey("TienNghi")]
        public string Ma_TienNghi { get; set; }

        [Range(1, 100, ErrorMessage = "Số lượng phải từ 1 đến 100")]
        public int SoLuong { get; set; }

        // Navigation properties
        public virtual Phong Phong { get; set; }
        public virtual TienNghi TienNghi { get; set; }
    }
}
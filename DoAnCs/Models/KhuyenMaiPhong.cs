using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnCs.Models
{
    [PrimaryKey(nameof(Ma_KM), nameof(Ma_Phong), nameof(ID_Homestay))]
    public class KhuyenMaiPhong
    {
        [Required]
        [ForeignKey("KhuyenMai")]
        public string Ma_KM { get; set; }

        [Required]
        [ForeignKey("Phong")]
        public string Ma_Phong { get; set; }
        [Required]
        [ForeignKey("Homestay")]
        public string ID_Homestay { get; set; } 

        // Navigation properties
        public virtual KhuyenMai KhuyenMai { get; set; }
        public virtual Phong Phong { get; set; }
        public virtual Homestay Homestay { get; set; }
    }
}

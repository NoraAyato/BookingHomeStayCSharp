using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnCs.Models
{
    [PrimaryKey(nameof(Ma_HD), nameof(Ma_KM))]
    public class ApDungKM
    {
        [ForeignKey("HoaDon")]
        public string Ma_HD { get; set; }

        [ForeignKey("KhuyenMai")]
        public string Ma_KM { get; set; }
        public virtual KhuyenMai KhuyenMai { get; set; }
        public virtual HoaDon HoaDon { get; set; }
    }
}
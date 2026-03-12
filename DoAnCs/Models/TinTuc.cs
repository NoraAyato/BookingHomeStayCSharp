using DoAnCs.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace DoAnCs.Models
{
    public class TinTuc
    {
        [Key]
        [StringLength(20)]
        public string Ma_TinTuc { get; set; }
        [ForeignKey("ChuDe")]
        public string ID_ChuDe { get; set; }
        public string TieuDe { get; set; }
        public string NoiDung { get; set; }
        public string? HinhAnh { get; set; }
        public string TacGia { get; set; }
        public DateTime NgayDang { get; set; }
        public string TrangThai { get; set; }
        public virtual ChuDe ChuDe { get; set; }
        public virtual ICollection<BinhLuan> BinhLuans { get; set; }
    }
}
using DoAnCs.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class PhieuSuDungDV
{
    [Key]
    public string Ma_Phieu { get; set; }

    public string Ma_Phong { get; set; }
    public string Ma_PDPhong { get; set; }

    [ForeignKey("Ma_PDPhong, Ma_Phong")]
    public virtual ChiTietDatPhong ChiTietDatPhong { get; set; }
    public virtual ICollection<ChiTietPhieuDV> ChiTietPhieuDVs { get; set; }
}
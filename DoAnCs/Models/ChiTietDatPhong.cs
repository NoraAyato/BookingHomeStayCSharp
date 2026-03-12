using DoAnCs.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

[PrimaryKey(nameof(Ma_PDPhong), nameof(Ma_Phong))]
public class ChiTietDatPhong
{
    [ForeignKey("PhieuDatPhong")]
    public string Ma_PDPhong { get; set; }
    [ForeignKey("Phong")]
    public string Ma_Phong { get; set; }
   
    public DateTime NgayDen { get; set; }
    public DateTime NgayDi { get; set; }
    public virtual Phong Phong { get; set; }
    public virtual PhieuDatPhong PhieuDatPhong { get; set; }
    public virtual ICollection<PhieuSuDungDV> PhieuSuDungDVs { get; set; }
}
using DoAnCs.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

[PrimaryKey(nameof(Ma_Phieu), nameof(Ma_DV))]
public class ChiTietPhieuDV
{
    [ForeignKey("PhieuSuDungDV")]
    public string Ma_Phieu { get; set; }
    [ForeignKey("DichVu")]
    public string Ma_DV { get; set; }
   
    public decimal SoLuong { get; set; }
    public DateTime NgaySuDung { get; set; }
    public string ID_Homestay { get; set; }
    public virtual PhieuSuDungDV PhieuSuDungDV { get; set; }
    public virtual DichVu DichVu { get; set; }
    
}
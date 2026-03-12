using DoAnCs.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class ThanhToan
{
    [Key]
    [StringLength(20)]
    public string MaTT { get; set; }
    [ForeignKey("HoaDon")]
    public string MaHD { get; set; }
    public decimal SoTien { get; set; }
    public string PhuongThuc { get; set; }
    public DateTime NgayTT { get; set; }
    public string TrangThai { get; set; }
    public string NoiDung { get; set; }
    public virtual HoaDon HoaDon { get; set; }
}
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[PrimaryKey(nameof(ID_Loai), nameof(Ma_PhieuPT))]
public class ApDungPhuThu
{
    [ForeignKey("LoaiPhong")]
    public string ID_Loai { get; set; }
    [ForeignKey("PhieuPhuThu")]
    public string Ma_PhieuPT { get; set; }
    public DateTime NgayApDung { get; set; }
    public virtual PhieuPhuThu PhieuPhuThu { get; set; }
    public virtual LoaiPhong LoaiPhong { get; set; }
    
}
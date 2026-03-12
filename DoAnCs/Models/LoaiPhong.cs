using DoAnCs.Models;
using System.ComponentModel.DataAnnotations;

public class LoaiPhong
{
    [Key]
    [StringLength(20)]
    public string ID_Loai { get; set; }
  
    public string TenLoai { get; set; }
    [StringLength(50)]
    public string Mo_Ta { get; set; }
    public virtual ICollection<Phong> Phongs { get; set; }
    public virtual ICollection<ApDungPhuThu> ApDungPhuThus { get; set; }
}
using System.ComponentModel.DataAnnotations;

public class PhieuPhuThu
{
    [Key]
    public string Ma_PhieuPT { get; set; }
    public DateTime NgayPhuThu { get; set; }
    public string NoiDung { get; set; }
    public decimal PhiPhuThu { get; set; }

    public virtual ICollection<ApDungPhuThu> ApDungPhuThus { get; set; }
}
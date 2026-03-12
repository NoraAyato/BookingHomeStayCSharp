using DoAnCs.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class DichVu
{
    [Key]
    [StringLength(20)]
    public string Ma_DV { get; set; }
    public string Ten_DV { get; set; }
    public decimal DonGia { get; set; }
    public string HinhAnh { get; set; }
    [ForeignKey("Homestay")]
    public string ID_Homestay { get; set; }
   
    public virtual Homestay Homestay { get; set; }

    public virtual ICollection<ChiTietPhieuDV> ChiTietPhieuDVs { get; set; }

}
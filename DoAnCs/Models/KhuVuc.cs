using DoAnCs.Models;
using System.ComponentModel.DataAnnotations;

public class KhuVuc
{
    [Key]
    [StringLength(20)]
    public string Ma_KV { get; set; }
    public string Ten_KV { get; set; }

    public virtual ICollection<Homestay> Homestays { get; set; } = new List<Homestay>();
}
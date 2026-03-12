using DoAnCs.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnCs.Models
{
    public class ChuDe
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public string ID_ChuDe { get; set; }
    public string TenChuDe { get; set; }

    public virtual ICollection<TinTuc> TinTucs { get; set; }
    }
}
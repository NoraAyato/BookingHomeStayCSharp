using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace DoAnCs.Models
{
    public class ChinhSach
    {
        [Key]
        public string Ma_CS { get; set; }

        [Required]
        public string ID_Homestay { get; set; }

        [ForeignKey("ID_Homestay")]
        public virtual Homestay Homestay { get; set; }

        [Required]
        public string NhanPhong { get; set; }

        [Required]
        public string TraPhong { get; set; }

        [Required]
        public string HuyPhong { get; set; }

        [Required]
        public string BuaAn { get; set; }
    }
}

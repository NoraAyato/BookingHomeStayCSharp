using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnCs.Models
{
    public class BinhLuan
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Ma_BinhLuan { get; set; }

        [Required]
        [StringLength(500)]
        public string NoiDung { get; set; }

        public DateTime NgayTao { get; set; } = DateTime.Now;

        // FK đến TinTuc
        [Required]
        [StringLength(20)]
        [Column("Ma_TinTuc")] // Ánh xạ sang cột Ma_TinTuc
        [ForeignKey("TinTuc")]
        public string Ma_TinTuc { get; set; }
        public virtual TinTuc TinTuc { get; set; }

        // FK đến ApplicationUser
        [Required]
        [ForeignKey("User")]
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        // Bình luận cha (nếu là phản hồi)
        [ForeignKey("BinhLuanCha")]
        public int? BinhLuanChaId { get; set; }
        public virtual BinhLuan BinhLuanCha { get; set; }

        // Danh sách phản hồi
        [InverseProperty("BinhLuanCha")]
        public virtual ICollection<BinhLuan> PhanHois { get; set; }
    }
}
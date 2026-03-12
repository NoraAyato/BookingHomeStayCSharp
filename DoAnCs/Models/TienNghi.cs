using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace DoAnCs.Models
{
    public class TienNghi
    {
        [Key]
        [StringLength(20, ErrorMessage = "Mã tiện nghi không được vượt quá 20 ký tự")]
        public string Ma_TienNghi { get; set; }

        [Required(ErrorMessage = "Tên tiện nghi không được để trống")]
        [StringLength(100, ErrorMessage = "Tên tiện nghi không được vượt quá 100 ký tự")]
        public string TenTienNghi { get; set; }

        [StringLength(200, ErrorMessage = "Mô tả không được vượt quá 200 ký tự")]
        public string MoTa { get; set; }

        // Navigation property
        public virtual ICollection<ChiTietPhong> ChiTietPhongs { get; set; }
    }
}
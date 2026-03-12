using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnCs.Models
{
    public class KhuyenMai
    {
        [Key]
        [StringLength(20)]
        public string Ma_KM { get; set; }

        [Required(ErrorMessage = "Nội dung khuyến mãi không được để trống")]
        [StringLength(100)]
        public string NoiDung { get; set; }

        [Required(ErrorMessage = "Ngày bắt đầu là bắt buộc")]
        public DateTime NgayBatDau { get; set; }

        [Required(ErrorMessage = "Hạn sử dụng là bắt buộc")]
        public DateTime HSD { get; set; }

        [Required(ErrorMessage = "Chiết khấu là bắt buộc")]
        [Range(0, double.MaxValue)]
        public decimal ChietKhau { get; set; }

        [Required]
        public string LoaiChietKhau { get; set; } // "Percentage" hoặc "Fixed"

        [Range(1, int.MaxValue)]
        public int? SoDemToiThieu { get; set; }

        [Range(0, int.MaxValue)]
        public int? SoNgayDatTruoc { get; set; }
        [StringLength(200, ErrorMessage = "Đường dẫn hình ảnh không được vượt quá 200 ký tự")]
        public string? HinhAnh { get; set; }
        public bool ChiApDungChoKhachMoi { get; set; } = false;
        public bool ApDungChoTatCaPhong { get; set; } = false;

        [Required]
        [ForeignKey("NguoiTao")]
        public string NguoiTaoId { get; set; }

        [Required]
        [StringLength(50)]
        public string TrangThai { get; set; } = "Đang áp dụng";

        [Range(0, int.MaxValue)]
        public decimal SoLuong { get; set; }

        public DateTime NgayTao { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual ApplicationUser NguoiTao { get; set; }
        public virtual ICollection<KhuyenMaiPhong> KhuyenMaiPhongs { get; set; } = new List<KhuyenMaiPhong>();
        public virtual ICollection<ApDungKM> ApDungKMs { get; set; } = new List<ApDungKM>();
    }
}
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnCs.Models
{
    public class Phong
    {
        [Key]
        [StringLength(20, ErrorMessage = "Mã phòng không được vượt quá 20 ký tự")]
        public string Ma_Phong { get; set; }

        [Required(ErrorMessage = "Tên phòng không được để trống")]
        [StringLength(100, ErrorMessage = "Tên phòng không được vượt quá 100 ký tự")]
        public string TenPhong { get; set; }

        [Required(ErrorMessage = "Trạng thái không được để trống")]
        [StringLength(50, ErrorMessage = "Trạng thái không được vượt quá 50 ký tự")]
        public string TrangThai { get; set; } = "Trống";

        [Required(ErrorMessage = "ID Homestay không được để trống")]
        [ForeignKey("Homestay")]
        public string ID_Homestay { get; set; }

        [Required(ErrorMessage = "ID Loại phòng không được để trống")]
        [StringLength(20, ErrorMessage = "ID Loại phòng không được vượt quá 20 ký tự")]
        [ForeignKey("LoaiPhong")]
        public string ID_Loai { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Đơn giá phải lớn hơn 0")]
        public decimal DonGia { get; set; }

        [Range(1, 100, ErrorMessage = "Số người phải từ 1 đến 100")]
        public decimal SoNguoi { get; set; }
        public string? VirtualTour { get; set; }
        public virtual Homestay Homestay { get; set; }
        public virtual LoaiPhong LoaiPhong { get; set; }
        public virtual ICollection<HinhAnhPhong> HinhAnhPhongs { get; set; }
        public virtual ICollection<ChiTietPhong> ChiTietPhongs { get; set; }
        public virtual ICollection<ChiTietDatPhong> ChiTietDatPhongs { get; set; }
        public virtual ICollection<KhuyenMaiPhong> KhuyenMaiPhongs { get; set; } 
    }
}
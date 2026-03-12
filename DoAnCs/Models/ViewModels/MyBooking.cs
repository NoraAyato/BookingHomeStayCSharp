using System.Text.Json.Serialization;

namespace DoAnCs.Models.ViewModels
{
    public class MyBooking
    {
        public class MyBookingDTO
        {
            [JsonPropertyName("PhieuDatPhongs")]
            public List<PhieuDatPhongDTO> PhieuDatPhongs { get; set; }

            [JsonPropertyName("CurrentPage")]
            public int CurrentPage { get; set; }

            [JsonPropertyName("TotalPages")]
            public int TotalPages { get; set; }
        }

        public class PhieuDatPhongDTO
        {
            [JsonPropertyName("maPDPhong")]
            public string MaPDPhong { get; set; }

            [JsonPropertyName("ngayLap")]
            public DateTime NgayLap { get; set; }

            [JsonPropertyName("trangThai")]
            public string TrangThai { get; set; }

            [JsonPropertyName("chiTietDatPhongs")]
            public List<ChiTietDatPhongDTO> ChiTietDatPhongs { get; set; }
        }

        public class ChiTietDatPhongDTO
        {
            [JsonPropertyName("Phong")]
            public PhongDTO Phong { get; set; }

            [JsonPropertyName("NgayDen")]
            public DateTime NgayDen { get; set; }

            [JsonPropertyName("NgayDi")]
            public DateTime NgayDi { get; set; }
        }

        public class PhongDTO
        {
            [JsonPropertyName("TenPhong")]
            public string TenPhong { get; set; }

            [JsonPropertyName("Homestay")]
            public HomestayDTO Homestay { get; set; }

            [JsonPropertyName("HinhAnhPhongs")]
            public List<HinhAnhPhongDTO> HinhAnhPhongs { get; set; }
        }

        public class HomestayDTO
        {
            [JsonPropertyName("Ten_Homestay")]
            public string TenHomestay { get; set; }
        }

        public class HinhAnhPhongDTO
        {
            [JsonPropertyName("UrlAnh")]
            public string UrlAnh { get; set; }

            [JsonPropertyName("LaAnhChinh")]
            public bool LaAnhChinh { get; set; }
        }
    }
}

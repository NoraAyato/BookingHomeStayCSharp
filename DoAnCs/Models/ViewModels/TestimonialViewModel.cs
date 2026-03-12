namespace DoAnCs.Models.ViewModels
{
    public class TestimonialViewModel
    {
        public string UserName { get; set; } // Tên khách hàng
        public string ProfilePicture { get; set; } // URL ảnh đại diện
        public float Rating { get; set; } // Điểm đánh giá (1-5)
        public string Content { get; set; } // Nội dung đánh giá
        public string HomestayName { get; set; } // Tên homestay
        public string Location { get; set; } // Địa điểm homestay
    }
}

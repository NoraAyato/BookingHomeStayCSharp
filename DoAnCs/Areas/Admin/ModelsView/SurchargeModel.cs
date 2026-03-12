namespace DoAnCs.Areas.Admin.ModelsView
{
    public class SurchargeModel
    {
        public string Ma_PhieuPT { get; set; }
        public string NgayPhuThu { get; set; }
        public string NoiDung { get; set; }
        public decimal PhiPhuThu { get; set; } // Phần trăm (ví dụ: 20)
        public List<string> RoomTypeIds { get; set; } = new List<string>();
    }
}

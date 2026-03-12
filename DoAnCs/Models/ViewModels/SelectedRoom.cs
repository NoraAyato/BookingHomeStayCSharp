namespace DoAnCs.Models.ViewModels
{
    public class SelectedRoom
    {
        public string MaPhong { get; set; }
        public string TenPhong { get; set; }
        public string HinhAnh { get; set; }
        public decimal DonGia { get; set; }
        public List<SelectedService> SelectedServices { get; set; } = new List<SelectedService>();
        public decimal TotalPhuThu { get; internal set; }
    }
}

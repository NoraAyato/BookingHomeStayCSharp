using Nest;

namespace DoAnCs.Models.ViewModels
{
    public class TinTucDocument
    {
        public string Ma_TinTuc { get; set; }
        public string TieuDe { get; set; }
        public string NoiDung { get; set; }
        public string ID_ChuDe { get; set; }
        public string TenChuDe { get; set; }
        public string TacGia { get; set; }
        public DateTime NgayDang { get; set; }
        public CompletionField Suggest { get; set; }
    }
}

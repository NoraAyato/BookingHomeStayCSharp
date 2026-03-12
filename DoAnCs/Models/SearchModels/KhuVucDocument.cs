using Nest;

namespace DoAnCs.Models.SearchModels
{
    public class KhuVucDocument
    {
        public string Ma_KV { get; set; }
        public string Ten_KV { get; set; }
        public CompletionField Suggest { get; set; } // Trường gợi ý
    }
}

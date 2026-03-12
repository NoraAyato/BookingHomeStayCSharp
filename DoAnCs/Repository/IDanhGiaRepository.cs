using DoAnCs.Models;

namespace DoAnCs.Repository
{
    public interface IDanhGiaRepository
    {
        Task<IEnumerable<DanhGia>> GetAllAsync();
        Task<DanhGia> GetByIdAsync(string idDG);
        Task<DanhGia> GetByMapdpAsync(string maPdp);
        Task AddAsync(DanhGia danhGia);
        Task UpdateAsync(DanhGia danhGia);
        Task DeleteAsync(string idDG);
        Task<bool> ExistsAsync(string userId , string maPdp);
        Task<IEnumerable<DanhGia>> SearchAsync(string searchString, string idHomestay, DateTime? startDate, DateTime? endDate, short? minRating, short? maxRating);
        Task<IEnumerable<DanhGia>> SearchAsync(string searchString,List<string> homestayIds,DateTime? startDate,DateTime? endDate,short? minRating,short? maxRating);
        Task<List<DanhGia>> GetTopTestimonialsFromDistinctAreasAsync(int count);
    }
}

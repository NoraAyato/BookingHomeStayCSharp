using DoAnCs.Areas.Admin.ModelsView;

namespace DoAnCs.Repository
{
    public interface IServiceRepository
    {
        Task<IEnumerable<DichVu>> GetAllAsync();
        Task<DichVu> GetByIdAsync(string maDV);
        Task<IEnumerable<DichVu>> GetByHomestayAsync(string idHomestay);
        Task<IEnumerable<DichVu>> GetMinimalByHomestayAsync(string idHomestay);
        Task<int> CountByHomestayAsync(string idHomestay);
        Task<bool> IsServiceNameExistsAsync(string tenDV, string idHomestay, string excludeMaDV = null); 
        Task<IEnumerable<DichVu>> GetByHomestayAsync(string idHomestay, int pageNumber = 1, int pageSize = 10);
        Task AddAsync(DichVu dichVu);
        Task UpdateAsync(DichVu dichVu);
        Task DeleteAsync(string maDV);
        Task <int> CountAllAsync();
        Task<List<DichVu>> GetByHostIdAsync(string hostId);
        Task<List<PopularService>> GetPopularServicesAsync();
    }
}

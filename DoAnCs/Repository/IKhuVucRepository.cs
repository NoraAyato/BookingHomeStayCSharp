using DoAnCs.Areas.Admin.ModelsView;

namespace DoAnCs.Repository
{
    public interface IKhuVucRepository
    {
        Task<KhuVuc> GetByNameAsync(string name);
        Task<IEnumerable<object>> SearchByNameAsync(string term);
        Task<IEnumerable<KhuVuc>> GetAllAsync();
        Task<KhuVuc> GetByIdAsync(string id);
        Task AddAsync(KhuVuc khuVuc);
        Task UpdateAsync(KhuVuc khuVuc);
        Task DeleteAsync(string id);
        Task<bool> ExistsAsync(string id);
        Task<bool> HasHomestaysAsync(string id);
        Task<List<PopularArea>> GetPopularAreasAsync();

    }
}

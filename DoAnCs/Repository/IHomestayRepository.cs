namespace DoAnCs.Repository
{
    using DoAnCs.Areas.Admin.ModelsView;
    using DoAnCs.Models;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IHomestayRepository
    {
        Task<IEnumerable<Homestay>> GetAllAsync();
        Task<IEnumerable<Homestay>> GetPaginatedAsync(int pageNumber, int pageSize, string searchString = null, string locationFilter = null, string statusFilter = null, string sortOrder = null);
        IQueryable<Homestay> GetAllQueryable();
        Task<Homestay> GetByIdAsync(string id);
        Task<int> CountAsync(string searchString = null, string locationFilter = null, string statusFilter = null);
        Task<Homestay> GetByIdWithDetailsAsync(string id);
        Task AddAsync(Homestay homestay);
        Task UpdateAsync(Homestay homestay);
        Task DeleteAsync(string id);
        Task<bool> ExistsAsync(string id);
        Task<int> CountActiveHomestaysAsync();
        Task<IEnumerable<Homestay>> GetTopRatedHomestaysAsync(int count);
        Task<IEnumerable<Homestay>> GetHomestaysByOwnerAsync(string ownerId);

        Task<IEnumerable<Homestay>> GetPaginatedByOwnerAsync(string ownerId, int pageNumber, int pageSize, string searchString = null, string locationFilter = null, string statusFilter = null, string sortOrder = null);
        Task<int> CountByOwnerAsync(string ownerId, string searchString = null, string locationFilter = null, string statusFilter = null);
        Task<List<PopularHomestay>> GetPopularHomestaysAsync();
    }
}

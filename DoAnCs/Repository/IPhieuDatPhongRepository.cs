using DoAnCs.Models;

namespace DoAnCs.Repository
{
    public interface IPhieuDatPhongRepository
    {
        Task<IEnumerable<PhieuDatPhong>> GetAllAsync();
        Task<PhieuDatPhong> GetByIdAsync(string maPDPhong);
        Task AddAsync(PhieuDatPhong phieuDatPhong);
        Task UpdateAsync(PhieuDatPhong phieuDatPhong);
        Task DeleteAsync(string maPDPhong);
        Task<(IEnumerable<PhieuDatPhong> Data, int TotalCount)> GetByUserIdWithPaginationAsync(string userId, int pageNumber, int pageSize);
        Task<bool> HasBookingAsync(string userId);
        Task<List<PhieuDatPhong>> GetByHostIdAsync(string hostId);
    }
}

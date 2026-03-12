using DoAnCs.Models;

namespace DoAnCs.Repository
{
    public interface IHopDongRepository
    {
        Task<IEnumerable<HopDong>> GetAllAsync();
        IQueryable<HopDong> GetHopDongQuery();
        Task<(IEnumerable<HopDong> hopDongs, int totalRecords)> SearchAsync(string searchQuery = "", string statusFilter = "all", string dateRange = "", int pageNumber = 1, int pageSize = 10);
        Task<(IEnumerable<HopDong> hopDongs, int totalRecords)> SearchForHostAsync(string userId,string searchQuery = "",string statusFilter = "all",string dateRange = "",int pageNumber = 1,int pageSize = 1);
        Task<HopDong> GetByIdAsync(string maHopDong);
        Task<object> GetStatusStatisticsAsync(string searchQuery = "", string dateRange = "");
        Task<object> GetStatusStatisticsForHostAsync(string userId, string searchQuery = "", string dateRange = "");
        Task<IEnumerable<HopDong>> GetByStatusAsync(string trangThai);
        Task<IEnumerable<HopDong>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task AddAsync(HopDong hopDong);
        Task<bool> UpdateAsync(HopDong hopDong);
        Task<bool> DeleteAsync(string maHopDong);
        Task<bool> UpdateStatusAsync(string maHopDong, string trangThai);
        Task<bool> CreateCancellationAsync(string maHopDong, string nguoiHuy, string lyDoHuy);

        // Quản lý PhieuHuyHopDong
        Task<IEnumerable<PhieuHuyHopDong>> GetAllCancellationRequestsAsync();
        Task<PhieuHuyHopDong> GetCancellationByIdAsync(string maPhieuHuy);
        Task<IEnumerable<PhieuHuyHopDong>> GetCancellationsByHopDongAsync(string maHopDong);
        Task<bool> UpdateCancellationStatusAsync(string maPhieuHuy, string trangThai);

        // Lưu thay đổi
        Task<int> SaveChangesAsync();
    }
}

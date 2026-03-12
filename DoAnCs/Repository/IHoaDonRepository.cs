using DoAnCs.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DoAnCs.Repository
{
    public interface IHoaDonRepository
    {
        Task<IEnumerable<HoaDon>> GetAllAsync();
        Task<HoaDon> GetByIdAsync(string maHD);
        Task AddAsync(HoaDon hoaDon);
        Task UpdateAsync(HoaDon hoaDon);
        Task UpdateStatusAsync(string maHD, string trangThai);
        Task DeleteAsync(string maHD);
        Task<IEnumerable<HoaDon>> GetByUserAsync(string maND);
        Task<bool> HasUnpaidInvoiceAsync(string maPDPhong);
        Task<HoaDon> GetUnpaidInvoiceAsync(string maPDPhong);
        Task<IEnumerable<ChiTietHoaDon>> GetByHoaDonAsync(string maHD);
        Task<HoaDon> GetByPhieuDatPhongAsync(string maPDPhong);
        Task<List<HoaDon>> GetByHostIdAsync(string hostId);
    }
}
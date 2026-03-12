using DoAnCs.Models;
using System.Threading.Tasks;

namespace DoAnCs.Repository
{
    public interface IHuyPhongRepository
        
    {
        // Lấy tất cả phiếu hủy phòng
        Task<IEnumerable<PhieuHuyPhong>> GetAllAsync();
        // Thêm phiếu hủy phòng
        Task AddAsync(PhieuHuyPhong phieuHuyPhong);

        // Lấy phiếu hủy phòng theo mã
        Task<PhieuHuyPhong> GetByIdAsync(string maPHP);

        // Lấy phiếu hủy phòng theo mã phiếu đặt phòng
        Task<PhieuHuyPhong> GetByMaPDPhongAsync(string maPDPhong);

        // Kiểm tra xem phiếu đặt phòng đã có phiếu hủy chưa
        Task<bool> HasCancellationAsync(string maPDPhong);

        // Cập nhật trạng thái phiếu hủy phòng
        Task UpdateAsync(PhieuHuyPhong phieuHuyPhong);

        // Lấy danh sách phiếu hủy phòng của người dùng
        Task<IEnumerable<PhieuHuyPhong>> GetByUserIdAsync(string userId);
    }
}
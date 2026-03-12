using DoAnCs.Models;
using Microsoft.AspNetCore.Identity;

namespace DoAnCs.Repository
{
    public interface IUserRepository
    {
        // Lấy tất cả người dùng
        Task<IEnumerable<ApplicationUser>> GetAllAsync();
        Task<(IEnumerable<ApplicationUser>, int)> SearchAsync(string searchQuery, string statusFilter, int pageNumber, int pageSize);

        // Lấy người dùng theo ID
        Task<ApplicationUser> GetByIdAsync(string id);

        // Lấy người dùng theo Email
        Task<ApplicationUser> GetByEmailAsync(string email);

        // Tìm kiếm người dùng theo trạng thái
        Task<IEnumerable<ApplicationUser>> GetByStatusAsync(string trangThai);

        // Thêm người dùng mới
        Task<IdentityResult> AddAsync(ApplicationUser user, string password);

        // Cập nhật người dùng
        Task<IdentityResult> UpdateAsync(ApplicationUser user);

        // Xóa người dùng
        Task<IdentityResult> DeleteAsync(string id);

        // Cập nhật trạng thái người dùng
        Task<IdentityResult> UpdateStatusAsync(string id, string trangThai);

        // Cập nhật tích điểm
        Task<IdentityResult> UpdateTichDiemAsync(string id, decimal tichDiem);
    }
}

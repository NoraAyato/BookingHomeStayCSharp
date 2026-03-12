using DoAnCs.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DoAnCs.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public UserRepository(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // Lấy tất cả người dùng
        public async Task<IEnumerable<ApplicationUser>> GetAllAsync()
        {
            return await _context.Users
                .Include(u => u.PhieuDatPhongs)
                .Include(u => u.Homestays)
                .Include(u => u.DanhGias)
                .Include(u => u.HoaDons)
                .ToListAsync();
        }
       
        public async Task<(IEnumerable<ApplicationUser>, int)> SearchAsync(string searchQuery, string statusFilter, int pageNumber, int pageSize)
        {
            var query = _context.Users.AsQueryable();

            // Tìm kiếm theo FullName hoặc Email
            if (!string.IsNullOrEmpty(searchQuery))
            {
                searchQuery = searchQuery.ToLower();
                query = query.Where(u => u.FullName.ToLower().Contains(searchQuery) || u.Email.ToLower().Contains(searchQuery));
            }

            // Lọc theo trạng thái
            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "all")
            {
                query = query.Where(u => u.TrangThai == statusFilter);
            }

            // Đếm tổng số bản ghi
            int totalRecords = await query.CountAsync();

            // Phân trang
            query = query.OrderBy(u => u.NgayTao) // Sắp xếp mặc định theo ngày tạo
                         .Skip((pageNumber - 1) * pageSize)
                         .Take(pageSize);

            // Chỉ lấy các cột cần thiết
            var users = await query.Select(u => new ApplicationUser
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Address = u.Address,
                DateOfBirth = u.DateOfBirth,
                ProfilePicture = u.ProfilePicture,
                NgayTao = u.NgayTao,
                TrangThai = u.TrangThai
            }).ToListAsync();

            return (users, totalRecords);
        }
        // Lấy người dùng theo ID
        public async Task<ApplicationUser> GetByIdAsync(string id)
        {
            return await _context.Users
                .Include(u => u.PhieuDatPhongs)
                .Include(u => u.Homestays)
                .Include(u => u.DanhGias)
                .Include(u => u.HoaDons)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        // Lấy người dùng theo Email
        public async Task<ApplicationUser> GetByEmailAsync(string email)
        {
            return await _userManager.FindByEmailAsync(email);
        }

        // Tìm kiếm người dùng theo trạng thái
        public async Task<IEnumerable<ApplicationUser>> GetByStatusAsync(string trangThai)
        {
            return await _context.Users
                .Where(u => u.TrangThai == trangThai)
                .Include(u => u.PhieuDatPhongs)
                .Include(u => u.Homestays)
                .Include(u => u.DanhGias)
                .Include(u => u.HoaDons)
                .ToListAsync();
        }

        // Thêm người dùng mới
        public async Task<IdentityResult> AddAsync(ApplicationUser user, string password)
        {
            user.NgayTao = DateTime.Now;
            user.TrangThai = "Hoạt động"; // Trạng thái mặc định
            user.tichdiem = user.tichdiem ?? 0; // Tích điểm mặc định là 0 nếu không có giá trị

            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                //Có thể thêm logic để gán role mặc định nếu cần
                await _userManager.AddToRoleAsync(user, "Customer");
            }
            return result;
        }

        // Cập nhật người dùng
        public async Task<IdentityResult> UpdateAsync(ApplicationUser user)
        {
            var existingUser = await _userManager.FindByIdAsync(user.Id);
            if (existingUser == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "Không tìm thấy người dùng" });
            }

            // Cập nhật các thuộc tính
            existingUser.FullName = user.FullName;
            existingUser.Address = user.Address;
            existingUser.DateOfBirth = user.DateOfBirth;
            existingUser.ProfilePicture = user.ProfilePicture;
            existingUser.TrangThai = user.TrangThai;
            existingUser.tichdiem = user.tichdiem;

            var result = await _userManager.UpdateAsync(existingUser);
            return result;
        }

        // Xóa người dùng
        public async Task<IdentityResult> DeleteAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "Không tìm thấy người dùng" });
            }

            var result = await _userManager.DeleteAsync(user);
            return result;
        }

        // Cập nhật trạng thái người dùng
        public async Task<IdentityResult> UpdateStatusAsync(string id, string trangThai)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "Không tìm thấy người dùng" });
            }

            user.TrangThai = trangThai;
            var result = await _userManager.UpdateAsync(user);
            return result;
        }

        // Cập nhật tích điểm
        public async Task<IdentityResult> UpdateTichDiemAsync(string id, decimal tichDiem)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "Không tìm thấy người dùng" });
            }

            user.tichdiem = tichDiem;
            var result = await _userManager.UpdateAsync(user);
            return result;
        }
    }
}
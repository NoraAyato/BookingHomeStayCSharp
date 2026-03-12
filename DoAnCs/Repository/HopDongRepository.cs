using DoAnCs.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DoAnCs.Repository
{
    public class HopDongRepository : IHopDongRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public HopDongRepository(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _webHostEnvironment = webHostEnvironment ?? throw new ArgumentNullException(nameof(webHostEnvironment));
        }

        public IQueryable<HopDong> GetHopDongQuery()
        {
            return _context.HopDongs
                .Include(h => h.ApplicationUser)
                .Include(h => h.KhuVuc)
                .Include(h => h.PhieuHuyHopDongs);
        }

        public IQueryable<PhieuHuyHopDong> GetCancellationQuery()
        {
            return _context.PhieuHuyHopDongs
                .Include(p => p.HopDong)
                .ThenInclude(h => h.ApplicationUser);
        }

        // Quản lý HopDong
        public async Task<IEnumerable<HopDong>> GetAllAsync()
        {
            return await GetHopDongQuery().ToListAsync();
        }

        public async Task<(IEnumerable<HopDong> hopDongs, int totalRecords)> SearchAsync(string searchQuery = "", string statusFilter = "all", string dateRange = "", int pageNumber = 1, int pageSize = 10)
        {
            IQueryable<HopDong> query = GetHopDongQuery();

            // Lọc theo mã hợp đồng
            if (!string.IsNullOrEmpty(searchQuery))
            {
                query = query.Where(hd => hd.Ma_HopDong.Contains(searchQuery));
            }

            // Lọc theo trạng thái
            if (statusFilter != "all" && !string.IsNullOrEmpty(statusFilter))
            {
                query = query.Where(hd => hd.TrangThai == statusFilter);
            }

            // Lọc theo khoảng ngày gửi
            if (!string.IsNullOrEmpty(dateRange))
            {
                var dates = dateRange.Split(" to ");
                if (dates.Length == 2)
                {
                    if (DateTime.TryParse(dates[0], out var startDate) && DateTime.TryParse(dates[1], out var endDate))
                    {
                        endDate = endDate.AddDays(1); // Bao gồm cả ngày cuối
                        query = query.Where(hd => hd.NgayGui >= startDate && hd.NgayGui < endDate);
                    }
                }
            }

            // Đếm tổng số bản ghi
            int totalRecords = await query.CountAsync();

            // Phân trang
            var hopDongs = await query
                .OrderByDescending(hd => hd.NgayGui)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (hopDongs, totalRecords);
        }
        public async Task<(IEnumerable<HopDong> hopDongs, int totalRecords)> SearchForHostAsync(
        string userId,
        string searchQuery = "",
        string statusFilter = "all",
        string dateRange = "",
        int pageNumber = 1,
        int pageSize = 1)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentNullException(nameof(userId));
            }

            IQueryable<HopDong> query = GetHopDongQuery()
                .Where(hd => hd.Ma_ND == userId); 

            // Lọc theo mã hợp đồng
            if (!string.IsNullOrEmpty(searchQuery))
            {
                query = query.Where(hd => hd.Ma_HopDong.Contains(searchQuery));
            }

            // Lọc theo trạng thái
            if (statusFilter != "all" && !string.IsNullOrEmpty(statusFilter))
            {
                query = query.Where(hd => hd.TrangThai == statusFilter);
            }

            if (!string.IsNullOrEmpty(dateRange))
            {
                var dates = dateRange.Split(" to "); 
                if (dates.Length == 2)
                {
                    if (DateTime.TryParseExact(dates[0], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var startDate) &&
                        DateTime.TryParseExact(dates[1], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var endDate))
                    {
                        endDate = endDate.AddDays(1); // Bao gồm cả ngày cuối
                        query = query.Where(hd => hd.NgayGui >= startDate && hd.NgayGui < endDate);
                    }
                }
            }

            // Đếm tổng số bản ghi
            int totalRecords = await query.CountAsync();

            // Phân trang
            var hopDongs = await query
                .OrderByDescending(hd => hd.NgayGui)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (hopDongs, totalRecords);
        }
        public async Task<object> GetStatusStatisticsAsync(string searchQuery = "", string dateRange = "")
        {
            IQueryable<HopDong> query = _context.HopDongs;

            // Áp dụng các bộ lọc tương tự như SearchAsync
            if (!string.IsNullOrEmpty(searchQuery))
            {
                query = query.Where(hd => hd.Ma_HopDong.Contains(searchQuery));
            }

            if (!string.IsNullOrEmpty(dateRange))
            {
                var dates = dateRange.Split(" to ");
                if (dates.Length == 2)
                {
                    if (DateTime.TryParse(dates[0], out var startDate) && DateTime.TryParse(dates[1], out var endDate))
                    {
                        endDate = endDate.AddDays(1); // Bao gồm cả ngày cuối
                        query = query.Where(hd => hd.NgayGui >= startDate && hd.NgayGui < endDate);
                    }
                }
            }

            // Tính toán số lượng hợp đồng theo từng trạng thái
            var stats = await query
                .GroupBy(hd => hd.TrangThai)
                .Select(g => new { TrangThai = g.Key, Count = g.Count() })
                .ToListAsync();

            // Tạo đối tượng thống kê
            return new
            {
                pending = stats.FirstOrDefault(s => s.TrangThai == "Đang chờ duyệt")?.Count ?? 0,
                approved = stats.FirstOrDefault(s => s.TrangThai == "Đã duyệt")?.Count ?? 0,
                rejected = stats.FirstOrDefault(s => s.TrangThai == "Từ chối")?.Count ?? 0,
                cancelled = stats.FirstOrDefault(s => s.TrangThai == "Đã hủy")?.Count ?? 0
            };
        }
        public async Task<object> GetStatusStatisticsForHostAsync(string userId, string searchQuery = "", string dateRange = "")
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentNullException(nameof(userId));
            }

            IQueryable<HopDong> query = _context.HopDongs
                .Where(hd => hd.Ma_ND == userId); // Lọc hợp đồng theo Host

            // Áp dụng các bộ lọc tương tự như SearchAsync
            if (!string.IsNullOrEmpty(searchQuery))
            {
                query = query.Where(hd => hd.Ma_HopDong.Contains(searchQuery));
            }

            if (!string.IsNullOrEmpty(dateRange))
            {
                var dates = dateRange.Split(" to ");
                if (dates.Length == 2)
                {
                    if (DateTime.TryParse(dates[0], out var startDate) && DateTime.TryParse(dates[1], out var endDate))
                    {
                        endDate = endDate.AddDays(1); // Bao gồm cả ngày cuối
                        query = query.Where(hd => hd.NgayGui >= startDate && hd.NgayGui < endDate);
                    }
                }
            }

            // Tính toán số lượng hợp đồng theo từng trạng thái
            var stats = await query
                .GroupBy(hd => hd.TrangThai)
                .Select(g => new { TrangThai = g.Key, Count = g.Count() })
                .ToListAsync();

            // Tạo đối tượng thống kê
            return new
            {
                pending = stats.FirstOrDefault(s => s.TrangThai == "Đang chờ duyệt")?.Count ?? 0,
                approved = stats.FirstOrDefault(s => s.TrangThai == "Đã duyệt")?.Count ?? 0,
                rejected = stats.FirstOrDefault(s => s.TrangThai == "Từ chối")?.Count ?? 0,
                cancelled = stats.FirstOrDefault(s => s.TrangThai == "Đã hủy")?.Count ?? 0
            };
        }
        public async Task<HopDong> GetByIdAsync(string maHopDong)
        {
            return await GetHopDongQuery().FirstOrDefaultAsync(h => h.Ma_HopDong == maHopDong);
        }

        public async Task<IEnumerable<HopDong>> GetByStatusAsync(string trangThai)
        {
            return await GetHopDongQuery().Where(h => h.TrangThai == trangThai).ToListAsync();
        }

        public async Task<IEnumerable<HopDong>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await GetHopDongQuery().Where(h => h.NgayGui >= startDate && h.NgayGui <= endDate).ToListAsync();
        }

        public async Task AddAsync(HopDong hopDong)
        {
            try
            {
                await _context.HopDongs.AddAsync(hopDong);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi thêm hợp đồng: {ex.Message}", ex);
            }
        }

        public async Task<bool> UpdateAsync(HopDong hopDong)
        {
            try
            {
                if (hopDong.TrangThai == "Đã hủy")
                {
                    return false; // Không thể cập nhật hợp đồng đã hủy
                }
                if (hopDong.TrangThai == "Đã duyệt")
                {
                    var homestay = await _context.Homestays.FindAsync(hopDong.Ma_ND);
                    if(homestay != null && homestay.Ten_Homestay == hopDong.Ten_Homestay && homestay.DiaChi == hopDong.DiaChi && homestay.Ma_KV == hopDong.Ma_KV)
                    {
                        homestay.TrangThai = "Đang hoạt động";
                        // Tìm tất cả các phòng thuộc Homestay
                        var phongs = await _context.Phongs
                            .Where(p => p.ID_Homestay == homestay.ID_Homestay)
                            .ToListAsync();            
                        foreach (var phong in phongs)
                        {
                            phong.TrangThai = "Hoạt động";
                        }
                    }

                }
                _context.HopDongs.Update(hopDong);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> DeleteAsync(string maHopDong)
        {
            try
            {
                var hopDong = await _context.HopDongs.FindAsync(maHopDong);
                if (hopDong != null)
                {
                    // Xóa hình ảnh nếu tồn tại
                    if (!string.IsNullOrEmpty(hopDong.HinhAnh))
                    {
                        var filePath = Path.Combine(_webHostEnvironment.WebRootPath, hopDong.HinhAnh.TrimStart('/'));
                        if (File.Exists(filePath))
                        {
                            File.Delete(filePath);
                        }
                    }

                    _context.HopDongs.Remove(hopDong);
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> UpdateStatusAsync(string maHopDong, string trangThai)
        {
            try
            {
                var hopDong = await _context.HopDongs.FindAsync(maHopDong);
                if (hopDong != null)
                {
                    hopDong.TrangThai = trangThai;
                    if (trangThai == "Đã duyệt")
                    {
                        hopDong.NgayDuyet = DateTime.Now;
                    }
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> CreateCancellationAsync(string maHopDong, string nguoiHuy, string lyDoHuy)
        {
            try
            {
                var hopDong = await _context.HopDongs.FindAsync(maHopDong);
                if (hopDong != null)
                {
                    if (hopDong.TrangThai == "Đã hủy")
                    {
                        return false;
                    }

                    var phieuHuy = new PhieuHuyHopDong
                    {
                        Ma_PhieuHuy = Guid.NewGuid().ToString(),
                        Ma_HopDong = maHopDong,
                        NguoiHuy = nguoiHuy,
                        LyDoHuy = lyDoHuy,
                        NgayHuy = DateTime.Now,
                        TrangThai = "Đã hủy"
                    };

                    hopDong.TrangThai = "Đã hủy";
                    // Tìm Homestay liên quan
                    var homestay = await _context.Homestays
                        .FirstOrDefaultAsync(h => h.Ma_ND == hopDong.Ma_ND
                                              && h.Ten_Homestay == hopDong.Ten_Homestay
                                              && h.DiaChi == hopDong.DiaChi
                                              && h.Ma_KV == hopDong.Ma_KV);

                    if (homestay != null)
                    {
                        // Cập nhật trạng thái Homestay thành "Ngừng hoạt động"
                        homestay.TrangThai = "Ngừng hoạt động";

                        // Tìm tất cả các phòng thuộc Homestay
                        var phongs = await _context.Phongs
                            .Where(p => p.ID_Homestay == homestay.ID_Homestay)
                            .ToListAsync();

                        // Cập nhật trạng thái các phòng thành "Bảo trì"
                        foreach (var phong in phongs)
                        {
                            phong.TrangThai = "Bảo trì";
                        }
                    }
                    if (!string.IsNullOrEmpty(hopDong.HinhAnh))
                    {
                        var filePath = Path.Combine(_webHostEnvironment.WebRootPath, hopDong.HinhAnh.TrimStart('/'));
                        if (File.Exists(filePath))
                        {
                            File.Delete(filePath);
                        }
                    }
                    
                    await _context.PhieuHuyHopDongs.AddAsync(phieuHuy);
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // Quản lý PhieuHuyHopDong
        public async Task<IEnumerable<PhieuHuyHopDong>> GetAllCancellationRequestsAsync()
        {
            return await GetCancellationQuery().ToListAsync();
        }

        public async Task<PhieuHuyHopDong> GetCancellationByIdAsync(string maPhieuHuy)
        {
            return await GetCancellationQuery().FirstOrDefaultAsync(p => p.Ma_PhieuHuy == maPhieuHuy);
        }

        public async Task<IEnumerable<PhieuHuyHopDong>> GetCancellationsByHopDongAsync(string maHopDong)
        {
            return await GetCancellationQuery().Where(p => p.Ma_HopDong == maHopDong).ToListAsync();
        }

        public async Task<bool> UpdateCancellationStatusAsync(string maPhieuHuy, string trangThai)
        {
            try
            {
                var phieuHuy = await _context.PhieuHuyHopDongs.FindAsync(maPhieuHuy);
                if (phieuHuy != null)
                {
                    phieuHuy.TrangThai = trangThai;
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
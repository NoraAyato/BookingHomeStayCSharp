using DoAnCs.Areas.Admin.ModelsView;
using Microsoft.EntityFrameworkCore;

namespace DoAnCs.Repository
{
    public class ServiceRepository : IServiceRepository
    {
        private readonly ApplicationDbContext _context;

        public ServiceRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<bool> IsServiceNameExistsAsync(string tenDV, string idHomestay, string excludeMaDV = null)
        {
          var query = _context.DichVus
                    .Where(dv => dv.ID_Homestay == idHomestay && dv.Ten_DV == tenDV);

                if (!string.IsNullOrEmpty(excludeMaDV))
                {
                    query = query.Where(dv => dv.Ma_DV != excludeMaDV);
                }

                return await query.AnyAsync();
        }
        // Lấy tất cả dịch vụ
        public async Task<int> CountAllAsync()
        {
            try
            {
                return await _context.DichVus.CountAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CountAllTinTucAsync: {ex.Message}");
                throw;
            }
        }
        public async Task<IEnumerable<DichVu>> GetAllAsync()
        {
            return await _context.DichVus
                .Include(dv => dv.Homestay)
                .Include(dv => dv.ChiTietPhieuDVs)
                .ToListAsync();
        }

        // Lấy dịch vụ theo ID
        public async Task<DichVu> GetByIdAsync(string maDV)
        {
            return await _context.DichVus
                .Include(dv => dv.Homestay)
                .Include(dv => dv.ChiTietPhieuDVs)
                .FirstOrDefaultAsync(dv => dv.Ma_DV == maDV);
        }

        // Lấy danh sách dịch vụ theo homestay
        public async Task<IEnumerable<DichVu>> GetByHomestayAsync(string idHomestay)
        {
            return await _context.DichVus
                .Include(dv => dv.Homestay)
                .Include(dv => dv.ChiTietPhieuDVs)
                .Where(dv => dv.ID_Homestay == idHomestay)
                .ToListAsync();
        }
        public async Task<IEnumerable<DichVu>> GetByHomestayAsync(string idHomestay, int pageNumber = 1, int pageSize = 10)
        {
            return await _context.DichVus
                .Include(dv => dv.Homestay)
                .Where(dv => dv.ID_Homestay == idHomestay)
                .OrderBy(dv => dv.Ten_DV)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        // Thêm dịch vụ mới
        public async Task AddAsync(DichVu dichVu)
        {
            try
            {
                if (await IsServiceNameExistsAsync(dichVu.Ten_DV, dichVu.ID_Homestay))
                {
                    throw new InvalidOperationException("Tên dịch vụ đã tồn tại trong homestay này");
                }

                if (await _context.DichVus.AnyAsync(dv => dv.Ma_DV == dichVu.Ma_DV))
                {
                    throw new InvalidOperationException("Mã dịch vụ đã tồn tại");
                }

                if (!await _context.Homestays.AnyAsync(h => h.ID_Homestay == dichVu.ID_Homestay))
                {
                    throw new InvalidOperationException("Homestay không tồn tại");
                }

                await _context.DichVus.AddAsync(dichVu);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
              
                throw new Exception("Lỗi khi lưu dịch vụ vào cơ sở dữ liệu. Vui lòng kiểm tra dữ liệu đầu vào.", ex);
            }
            catch (Exception ex)
            {
                var errorMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    errorMessage += $"\nInner Exception: {ex.InnerException.Message}";
                }
                throw new Exception($"Lỗi khi thêm dịch vụ: {errorMessage}", ex);
            }
        }

        // Cập nhật dịch vụ
        public async Task UpdateAsync(DichVu dichVu)
        {
            try
            {
                var existingDichVu = await _context.DichVus
                    .FirstOrDefaultAsync(dv => dv.Ma_DV == dichVu.Ma_DV);

                if (existingDichVu == null)
                {
                    throw new Exception("Không tìm thấy dịch vụ để cập nhật");
                }

                if (await IsServiceNameExistsAsync(dichVu.Ten_DV, dichVu.ID_Homestay, dichVu.Ma_DV))
                {
                    throw new InvalidOperationException("Tên dịch vụ đã được sử dụng bởi dịch vụ khác trong homestay này");
                }

                if (!await _context.Homestays.AnyAsync(h => h.ID_Homestay == dichVu.ID_Homestay))
                {
                    throw new InvalidOperationException("Homestay không tồn tại");
                }

                existingDichVu.Ten_DV = dichVu.Ten_DV;
                existingDichVu.DonGia = dichVu.DonGia;
                existingDichVu.ID_Homestay = dichVu.ID_Homestay;
                existingDichVu.HinhAnh = dichVu.HinhAnh;

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
               
                throw new Exception("Lỗi khi lưu dịch vụ vào cơ sở dữ liệu. Vui lòng kiểm tra dữ liệu đầu vào.", ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi Thêm dịch vụ.", ex);
            }
        }
        public async Task<int> CountByHomestayAsync(string idHomestay)
        {
            try
            {
                return await _context.DichVus
                    .Where(dv => dv.ID_Homestay == idHomestay)
                    .CountAsync();
            }
            catch (Exception ex)
            {     
                throw;
            }
        }
        public async Task<IEnumerable<DichVu>> GetMinimalByHomestayAsync(string idHomestay)
        {
            return await _context.DichVus
                .Where(dv => dv.ID_Homestay == idHomestay)
                .Select(dv => new DichVu
                {
                    Ma_DV = dv.Ma_DV,
                    Ten_DV = dv.Ten_DV,
                    DonGia = dv.DonGia
                })
                .ToListAsync();
        }
        // Xóa dịch vụ
        public async Task DeleteAsync(string maDV)
        {
            try
            {
                var dichVu = await _context.DichVus
                    .Include(dv => dv.ChiTietPhieuDVs)
                    .FirstOrDefaultAsync(dv => dv.Ma_DV == maDV);

                if (dichVu == null)
                {
                    throw new Exception("Không tìm thấy dịch vụ để xóa");
                }

                // Xóa các bản ghi liên quan
                if (dichVu.ChiTietPhieuDVs != null)
                {
                    _context.ChiTietPhieuDVs.RemoveRange(dichVu.ChiTietPhieuDVs);
                }

                // Xóa file ảnh nếu có
                if (!string.IsNullOrEmpty(dichVu.HinhAnh))
                {
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", dichVu.HinhAnh.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                _context.DichVus.Remove(dichVu);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                var errorMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    errorMessage += $"\nInner Exception: {ex.InnerException.Message}";
                }
                throw new Exception($"Lỗi khi xóa dịch vụ: {errorMessage}", ex);
            }
        }
        public async Task<List<DichVu>> GetByHostIdAsync(string hostId)
        {
            return await _context.DichVus
                .Include(d => d.Homestay)
                    .ThenInclude(h => h.KhuVuc)
                .Where(d => d.Homestay.Ma_ND == hostId)
                .AsNoTracking()
                .ToListAsync();
        }
        public async Task<List<PopularService>> GetPopularServicesAsync()
        {
            return await _context.DichVus
                .GroupBy(dv => dv.Ten_DV)
                .Select(g => new PopularService
                {
                    Ten_DV = g.Key,
                    HomestayCount = g.Select(dv => dv.ID_Homestay).Distinct().Count()
                })
                .OrderByDescending(s => s.HomestayCount)
                .Take(5)
                .ToListAsync();
        }
    }
}

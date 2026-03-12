using DoAnCs.Models;
using Microsoft.EntityFrameworkCore;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DoAnCs.Repository
{
    public class PhongRepository : IPhongRepository
    {
        private readonly ApplicationDbContext _context;

        public PhongRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // Lấy tất cả phòng
        public async Task<IEnumerable<Phong>> GetAllAsync()
        {
            return await _context.Phongs
                .Include(p => p.Homestay)
                .Include(p => p.LoaiPhong)
                .Include(p => p.HinhAnhPhongs)
                .Include(p => p.ChiTietPhongs)
                .ToListAsync();
        }

        // Lấy phòng theo ID
        public async Task<Phong> GetByIdAsync(string maPhong)
        {
            return await _context.Phongs
                .Include(p => p.Homestay)
                .Include(p => p.LoaiPhong)
                .Include(p => p.HinhAnhPhongs)
                .Include(p => p.ChiTietPhongs)
                .FirstOrDefaultAsync(p => p.Ma_Phong == maPhong);
        }
        public async Task<List<Phong>> GetByIdsAsync(IEnumerable<string> maPhongs)
        {
            return await _context.Phongs.Include(p => p.HinhAnhPhongs)
                .Include(p => p.ChiTietDatPhongs)
                .Where(p => maPhongs.Contains(p.Ma_Phong))
                .ToListAsync();
        }
        // Lấy danh sách phòng theo homestay
        public async Task<IEnumerable<Phong>> GetByHomestayAsync(string idHomestay)
        {
            return await _context.Phongs
                .Include(p => p.Homestay)
                .Include(p => p.LoaiPhong)
                .Include(p => p.HinhAnhPhongs)
                .Include(p => p.ChiTietPhongs).ThenInclude(p=>p.TienNghi)
                .Where(p => p.ID_Homestay == idHomestay)
                .ToListAsync();
        }

        // Thêm phòng mới
        public async Task AddAsync(Phong phong)
        {
            try
            {
                // Thêm phòng và lưu
                await _context.Phongs.AddAsync(phong);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                var innerException = ex.InnerException?.Message ?? ex.Message;
                throw new Exception($"Lỗi khi lưu vào database: {innerException}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi thêm phòng: {ex.Message}", ex);
            }
        }

        // Cập nhật phòng
        public async Task UpdateAsync(Phong phong)
        {
            try
            {
                var existingPhong = await _context.Phongs
                    .Include(p => p.HinhAnhPhongs)
                    .Include(p => p.ChiTietPhongs)
                    .FirstOrDefaultAsync(p => p.Ma_Phong == phong.Ma_Phong);

                if (existingPhong == null)
                {
                    throw new Exception("Không tìm thấy phòng để cập nhật");
                }

                // Kiểm tra khóa ngoại
                var homestayExists = await _context.Homestays.AnyAsync(h => h.ID_Homestay == phong.ID_Homestay);
                if (!homestayExists)
                    throw new InvalidOperationException($"Homestay với ID {phong.ID_Homestay} không tồn tại");

                var loaiPhongExists = await _context.LoaiPhongs.AnyAsync(l => l.ID_Loai == phong.ID_Loai);
                if (!loaiPhongExists)
                    throw new InvalidOperationException($"Loại phòng với ID {phong.ID_Loai} không tồn tại");

                // Cập nhật các thuộc tính của phòng
                _context.Entry(existingPhong).CurrentValues.SetValues(phong);

                // Cập nhật HinhAnhPhongs
                if (phong.HinhAnhPhongs != null)
                {
                    _context.HinhAnhPhongs.RemoveRange(existingPhong.HinhAnhPhongs);
                    foreach (var hinhAnh in phong.HinhAnhPhongs)
                    {
                        if (string.IsNullOrEmpty(hinhAnh.MaPhong) || hinhAnh.MaPhong != phong.Ma_Phong)
                            throw new ArgumentException($"HinhAnhPhong.MaPhong phải khớp với Phong.Ma_Phong ({phong.Ma_Phong})");
                        if (string.IsNullOrEmpty(hinhAnh.UrlAnh))
                            throw new ArgumentException("HinhAnhPhong.UrlAnh không được rỗng");
                        _context.HinhAnhPhongs.Add(hinhAnh);
                    }
                }
                else
                {
                    _context.HinhAnhPhongs.RemoveRange(existingPhong.HinhAnhPhongs);
                }

                // Cập nhật ChiTietPhongs
                if (phong.ChiTietPhongs != null)
                {
                    _context.ChiTietPhongs.RemoveRange(existingPhong.ChiTietPhongs);
                    foreach (var chiTiet in phong.ChiTietPhongs)
                    {
                        if (string.IsNullOrEmpty(chiTiet.Ma_Phong) || chiTiet.Ma_Phong != phong.Ma_Phong)
                            throw new ArgumentException($"ChiTietPhong.Ma_Phong phải khớp với Phong.Ma_Phong ({phong.Ma_Phong})");
                        //if (string.IsNullOrEmpty(chiTiet.TenTienNghi))
                        //    throw new ArgumentException("Tên tiện nghi không được rỗng");
                        if (chiTiet.SoLuong <= 0)
                            throw new ArgumentException("Số lượng tiện nghi phải lớn hơn 0");
                        _context.ChiTietPhongs.Add(chiTiet);
                    }
                }
                else
                {
                    _context.ChiTietPhongs.RemoveRange(existingPhong.ChiTietPhongs);
                }

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                var innerException = ex.InnerException?.Message ?? ex.Message;
                throw new Exception($"Lỗi khi cập nhật phòng: {innerException}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi cập nhật phòng: {ex.Message}", ex);
            }
        }

        // Xóa phòng
        public async Task DeleteAsync(string maPhong)
        {
            try
            {
                var phong = await _context.Phongs
                    .Include(p => p.HinhAnhPhongs)
                    .Include(p => p.ChiTietPhongs)
                    .FirstOrDefaultAsync(p => p.Ma_Phong == maPhong);

                if (phong == null)
                {
                    throw new Exception("Không tìm thấy phòng để xóa");
                }

                // Xóa các bản ghi liên quan
                if (phong.HinhAnhPhongs != null)
                {
                    _context.HinhAnhPhongs.RemoveRange(phong.HinhAnhPhongs);
                }

                if (phong.ChiTietPhongs != null)
                {
                    _context.ChiTietPhongs.RemoveRange(phong.ChiTietPhongs);
                }

                _context.Phongs.Remove(phong);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                var innerException = ex.InnerException?.Message ?? ex.Message;
                throw new Exception($"Lỗi khi xóa phòng: {innerException}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi xóa phòng: {ex.Message}", ex);
            }
        }

        // Lấy danh sách loại phòng
        public async Task<IEnumerable<LoaiPhong>> GetLoaiPhongsAsync()
        {
            return await _context.LoaiPhongs.ToListAsync();
        }

        // Lấy chi tiết phòng theo mã phòng
        public async Task<IEnumerable<ChiTietPhong>> GetChiTietPhongsAsync(string maPhong)
        {
            return await _context.ChiTietPhongs
                .Where(ct => ct.Ma_Phong == maPhong)
                .ToListAsync();
        }

        // Lấy hình ảnh phòng theo mã phòng
        public async Task<IEnumerable<HinhAnhPhong>> GetHinhAnhPhongsAsync(string maPhong)
        {
            return await _context.HinhAnhPhongs
                .Where(ha => ha.MaPhong == maPhong)
                .ToListAsync();
        }
        public async Task<List<Phong>> GetByHostIdAsync(string hostId)
        {
            return await _context.Phongs
                .Include(p => p.Homestay)
                    .ThenInclude(h => h.KhuVuc)
                .Where(p => p.Homestay.Ma_ND == hostId)
                .AsNoTracking()
                .ToListAsync();
        }
        public async Task<bool> ExistsLoaiPhongAsync(string id)
        {
            return await _context.LoaiPhongs.AnyAsync(h => h.ID_Loai == id);
        }
        public async Task<bool> ExistsPhongAsync(string id)
        {
            return await _context.Phongs.AnyAsync(h => h.Ma_Phong == id);
        }
        public async Task<IEnumerable<(DateTime NgayDen, DateTime NgayDi)>> GetBookedDateRangesAsync(string maPhong)
        {
            try
            {
                var phong = await _context.Phongs
                                .Include(p => p.ChiTietDatPhongs)
                                    .ThenInclude(ct => ct.PhieuDatPhong)
                                .FirstOrDefaultAsync(p => p.Ma_Phong == maPhong);


                if (phong == null)
                {
                    throw new Exception($"Không tìm thấy phòng với mã {maPhong}");
                }
                var today = DateTime.Today;
                return phong.ChiTietDatPhongs
                    .Where(ct => ct.PhieuDatPhong.TrangThai != "Đã hủy" && ct.NgayDi >= today)
                    .Select(ct => (ct.NgayDen, ct.NgayDi))
                    .OrderBy(ct => ct.NgayDen)
                    .ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy danh sách khoảng thời gian đã đặt: {ex.Message}", ex);
            }
        }
    }
}
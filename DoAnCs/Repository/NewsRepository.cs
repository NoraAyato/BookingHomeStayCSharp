
using DoAnCs.Areas.Admin.ModelsView;
using DoAnCs.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DoAnCs.Repository
{
    public class NewsRepository : INewsRepository
    {
        private readonly ApplicationDbContext _context;

        public NewsRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // --- Quản lý ChuDe ---
        public async Task<int> CountAllTinTucAsync()
        {
            try
            {
                return await _context.TinTucs.CountAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CountAllTinTucAsync: {ex.Message}");
                throw;
            }
        }
        public async Task<IEnumerable<ChuDe>> GetAllChuDeAsync()
        {
            try
            {
                return await _context.ChuDes
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAllChuDeAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<ChuDe> GetChuDeByIdAsync(string idChuDe)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(idChuDe))
                {
                    Console.WriteLine("ID_ChuDe is null or empty");
                    return null;
                }

                return await _context.ChuDes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(cd => cd.ID_ChuDe == idChuDe);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetChuDeByIdAsync: {ex.Message}");
                throw;
            }
        }

        public async Task AddChuDeAsync(ChuDe chuDe)
        {
            try
            {
                if (chuDe == null)
                    throw new ArgumentNullException(nameof(chuDe));

                if (string.IsNullOrWhiteSpace(chuDe.TenChuDe))
                    throw new ArgumentException("Tên chủ đề không được để trống", nameof(chuDe.TenChuDe));

                await _context.ChuDes.AddAsync(chuDe);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException dbEx)
            {
                Console.WriteLine($"DbUpdateException in AddChuDeAsync: {dbEx.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AddChuDeAsync: {ex.Message}");
                throw;
            }
        }

        public async Task UpdateChuDeAsync(ChuDe chuDe)
        {
            try
            {
                if (chuDe == null)
                    throw new ArgumentNullException(nameof(chuDe));

                var existingChuDe = await _context.ChuDes
                    .FirstOrDefaultAsync(cd => cd.ID_ChuDe == chuDe.ID_ChuDe);

                if (existingChuDe == null)
                    throw new Exception("Không tìm thấy chủ đề để cập nhật");

                existingChuDe.TenChuDe = chuDe.TenChuDe;
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException dbEx)
            {
                Console.WriteLine($"DbUpdateException in UpdateChuDeAsync: {dbEx.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateChuDeAsync: {ex.Message}");
                throw;
            }
        }

        public async Task DeleteChuDeAsync(string idChuDe)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(idChuDe))
                    throw new ArgumentException("ID_ChuDe không được để trống", nameof(idChuDe));

                var chuDe = await _context.ChuDes
                    .FirstOrDefaultAsync(cd => cd.ID_ChuDe == idChuDe);

                if (chuDe == null)
                    throw new Exception("Không tìm thấy chủ đề để xóa");

                _context.ChuDes.Remove(chuDe);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException dbEx)
            {
                Console.WriteLine($"DbUpdateException in DeleteChuDeAsync: {dbEx.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DeleteChuDeAsync: {ex.Message}");
                throw;
            }
        }

        // --- Quản lý TinTuc ---

        public IQueryable<TinTuc> GetTinTucQueryable()
        {
            return _context.TinTucs
                .Include(t => t.ChuDe)
                .AsNoTracking();
        }

        public async Task<IEnumerable<TinTuc>> GetAllTinTucAsync()
        {
            try
            {
                return await GetTinTucQueryable()
                    .Include(t => t.BinhLuans)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<TinTuc> GetTinTucByIdAsync(string maTinTuc)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(maTinTuc))
                    return null;

                return await _context.TinTucs
                    .Include(t => t.ChuDe)
                    .Include(t => t.BinhLuans)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Ma_TinTuc == maTinTuc);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetTinTucByIdAsync: {ex.Message}");
                throw;
            }
        }

        public async Task AddTinTucAsync(TinTuc tinTuc)
        {
            try
            {
                if (tinTuc == null)
                    throw new ArgumentNullException(nameof(tinTuc));

                await _context.TinTucs.AddAsync(tinTuc);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException dbEx)
            {
                Console.WriteLine($"DbUpdateException in AddTinTucAsync: {dbEx.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AddTinTucAsync: {ex.Message}");
                throw;
            }
        }

        public async Task UpdateTinTucAsync(TinTuc tinTuc)
        {
            try
            {
                if (tinTuc == null)
                    throw new ArgumentNullException(nameof(tinTuc));

                var existingTinTuc = await _context.TinTucs
                    .FirstOrDefaultAsync(t => t.Ma_TinTuc == tinTuc.Ma_TinTuc);

                if (existingTinTuc == null)
                    throw new Exception("Không tìm thấy tin tức để cập nhật");

                existingTinTuc.ID_ChuDe = tinTuc.ID_ChuDe;
                existingTinTuc.TieuDe = tinTuc.TieuDe;
                existingTinTuc.NoiDung = tinTuc.NoiDung;
                existingTinTuc.HinhAnh = tinTuc.HinhAnh;
                existingTinTuc.TacGia = tinTuc.TacGia;
                existingTinTuc.NgayDang = tinTuc.NgayDang;
                existingTinTuc.TrangThai = tinTuc.TrangThai;

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException dbEx)
            {
                Console.WriteLine($"DbUpdateException in UpdateTinTucAsync: {dbEx.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateTinTucAsync: {ex.Message}");
                throw;
            }
        }

        public async Task DeleteTinTucAsync(string maTinTuc)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(maTinTuc))
                    throw new ArgumentException("Mã tin tức không được để trống", nameof(maTinTuc));

                var tinTuc = await _context.TinTucs
                    .FirstOrDefaultAsync(t => t.Ma_TinTuc == maTinTuc);

                if (tinTuc == null)
                    throw new Exception("Không tìm thấy tin tức để xóa");

                _context.TinTucs.Remove(tinTuc);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException dbEx)
            {
                Console.WriteLine($"DbUpdateException in DeleteTinTucAsync: {dbEx.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DeleteTinTucAsync: {ex.Message}");
                throw;
            }
        }

        // --- Quản lý BinhLuan ---

        public async Task<IEnumerable<BinhLuan>> GetBinhLuansByTinTucAsync(string maTinTuc)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(maTinTuc))
                    return new List<BinhLuan>();

                var comments = await _context.BinhLuans
                 .Include(b => b.User) // Include User for comment author
                 .Include(b => b.PhanHois) // Include replies
                     .ThenInclude(p => p.User) // Include User for reply authors
                 .Where(b => b.Ma_TinTuc == maTinTuc)
                 .ToListAsync();


                return comments;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetBinhLuansByTinTucAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<BinhLuan> GetBinhLuanByIdAsync(int maBinhLuan)
        {
            try
            {
                return await _context.BinhLuans
                    .Include(b => b.User)
                    .Include(b => b.PhanHois)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(b => b.Ma_BinhLuan == maBinhLuan);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetBinhLuanByIdAsync: {ex.Message}");
                throw;
            }
        }

        public async Task DeleteBinhLuanAsync(int maBinhLuan)
        {
            try
            {
                var binhLuan = await _context.BinhLuans
                    .Include(b => b.PhanHois)
                    .FirstOrDefaultAsync(b => b.Ma_BinhLuan == maBinhLuan);

                if (binhLuan == null)
                    throw new Exception("Không tìm thấy bình luận để xóa");

                if (binhLuan.PhanHois != null && binhLuan.PhanHois.Any())
                {
                    _context.BinhLuans.RemoveRange(binhLuan.PhanHois);
                }

                _context.BinhLuans.Remove(binhLuan);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException dbEx)
            {
                Console.WriteLine($"DbUpdateException in DeleteBinhLuanAsync: {dbEx.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DeleteBinhLuanAsync: {ex.Message}");
                throw;
            }
        }
        public async Task AddBinhLuanAsync(BinhLuan binhLuan)
        {
            try
            {
                if (binhLuan == null)
                    throw new ArgumentNullException(nameof(binhLuan));

                if (string.IsNullOrWhiteSpace(binhLuan.NoiDung))
                    throw new ArgumentException("Nội dung bình luận không được để trống", nameof(binhLuan.NoiDung));

                if (string.IsNullOrWhiteSpace(binhLuan.Ma_TinTuc))
                    throw new ArgumentException("Mã tin tức không được để trống", nameof(binhLuan.Ma_TinTuc));

                if (string.IsNullOrWhiteSpace(binhLuan.UserId))
                    throw new ArgumentException("ID người dùng không được để trống", nameof(binhLuan.UserId));

                // Kiểm tra xem tin tức có tồn tại không
                var tinTuc = await _context.TinTucs
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Ma_TinTuc == binhLuan.Ma_TinTuc);
                if (tinTuc == null)
                    throw new Exception("Tin tức không tồn tại");

                // Kiểm tra xem người dùng có tồn tại không
                var user = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == binhLuan.UserId);
                if (user == null)
                    throw new Exception("Người dùng không tồn tại");

                // Nếu là phản hồi, kiểm tra bình luận cha
                if (binhLuan.BinhLuanChaId.HasValue)
                {
                    var binhLuanCha = await _context.BinhLuans
                        .AsNoTracking()
                        .FirstOrDefaultAsync(b => b.Ma_BinhLuan == binhLuan.BinhLuanChaId.Value);
                    if (binhLuanCha == null)
                        throw new Exception("Bình luận cha không tồn tại");
                }

                binhLuan.NgayTao = DateTime.Now;
                await _context.BinhLuans.AddAsync(binhLuan);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException dbEx)
            {
                Console.WriteLine($"DbUpdateException in AddBinhLuanAsync: {dbEx.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AddBinhLuanAsync: {ex.Message}");
                throw;
            }
        }
        public async Task<List<PopularNews>> GetPopularNewsAsync()
        {
            try
            {
                return await _context.TinTucs
                    .Include(t => t.BinhLuans)
                    .Select(t => new PopularNews
                    {
                        Ma_TinTuc = t.Ma_TinTuc,
                        TieuDe = t.TieuDe,
                        CommentCount = t.BinhLuans.Count(),
                        HinhAnh = t.HinhAnh
                    })
                    .OrderByDescending(n => n.CommentCount)
                    .Take(5)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetPopularNewsAsync: {ex.Message}");
                throw;
            }
        }
    }
}
using DoAnCs.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DoAnCs.Repository
{
    public class PhieuDatPhongRepository : IPhieuDatPhongRepository
    {
        private readonly ApplicationDbContext _context;

        public PhieuDatPhongRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        private IQueryable<PhieuDatPhong> IncludeAll()
        {
            return _context.PhieuDatPhongs
                .Include(p => p.NguoiDung)
                .Include(p => p.ChiTietDatPhongs)
                    .ThenInclude(ct => ct.Phong)
                        .ThenInclude(ph => ph.Homestay)
                .Include(p => p.ChiTietDatPhongs)
                    .ThenInclude(ct => ct.Phong)
                        .ThenInclude(ph => ph.HinhAnhPhongs)
                .Include(p => p.ChiTietDatPhongs)
                    .ThenInclude(ct => ct.PhieuSuDungDVs)
                        .ThenInclude(ps => ps.ChiTietPhieuDVs)
                            .ThenInclude(ctp => ctp.DichVu);
        }

        public async Task<IEnumerable<PhieuDatPhong>> GetAllAsync()
        {
            return await IncludeAll().ToListAsync();
        }

        public async Task<PhieuDatPhong> GetByIdAsync(string maPDPhong)
        {
            return await IncludeAll()
                .AsSplitQuery()
                .FirstOrDefaultAsync(p => p.Ma_PDPhong == maPDPhong);
        }

        public async Task AddAsync(PhieuDatPhong phieuDatPhong)
        {
            await _context.PhieuDatPhongs.AddAsync(phieuDatPhong);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(PhieuDatPhong phieuDatPhong)
        {
            _context.PhieuDatPhongs.Update(phieuDatPhong);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(string maPDPhong)
        {
            var chiTietDatPhongs = await _context.ChiTietDatPhongs
                .Where(ct => ct.Ma_PDPhong == maPDPhong)
                .ToListAsync();

            var phieuSuDungDVs = await _context.PhieuSuDungDVs
                .Where(ps => ps.Ma_PDPhong == maPDPhong)
                .Include(ps => ps.ChiTietPhieuDVs)
                .ToListAsync();

            _context.ChiTietPhieuDVs.RemoveRange(phieuSuDungDVs.SelectMany(ps => ps.ChiTietPhieuDVs));
            _context.PhieuSuDungDVs.RemoveRange(phieuSuDungDVs);
            _context.ChiTietDatPhongs.RemoveRange(chiTietDatPhongs);

            var phieuHuyPhong = await _context.PhieuHuyPhongs.FindAsync(maPDPhong);
            if (phieuHuyPhong != null)
            {
                _context.PhieuHuyPhongs.Remove(phieuHuyPhong);
            }

            var phieuDatPhong = await _context.PhieuDatPhongs.FindAsync(maPDPhong);
            if (phieuDatPhong != null)
            {
                _context.PhieuDatPhongs.Remove(phieuDatPhong);
            }

            await _context.SaveChangesAsync(); 
        }

        public async Task<(IEnumerable<PhieuDatPhong> Data, int TotalCount)> GetByUserIdWithPaginationAsync(string userId, int pageNumber, int pageSize)
        {
            var query = IncludeAll().Where(p => p.Ma_ND == userId);

            int totalCount = await query.CountAsync();

            var data = await query
                .OrderByDescending(p => p.NgayLap)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (data, totalCount);
        }
        public async Task<List<PhieuDatPhong>> GetByHostIdAsync(string hostId)
        {
            return await _context.PhieuDatPhongs
                .Include(p => p.NguoiDung)
                .Include(p => p.ChiTietDatPhongs)
                    .ThenInclude(ct => ct.Phong)
                        .ThenInclude(ph => ph.Homestay)
                            .ThenInclude(h => h.KhuVuc)
                .Where(p => p.ChiTietDatPhongs.Any(ct => ct.Phong.Homestay.Ma_ND == hostId))
                .AsNoTracking()
                .ToListAsync();
        }
        public async Task<bool> HasBookingAsync(string userId)
        {
            return await _context.PhieuDatPhongs
                .AnyAsync(p => p.Ma_ND == userId && p.TrangThai == "Đã xác nhận");
        }
    }
}

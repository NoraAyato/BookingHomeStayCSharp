using DoAnCs.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DoAnCs.Repository
{
    public class DanhGiaRepository : IDanhGiaRepository
    {
        private readonly ApplicationDbContext _context;

        public DanhGiaRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<DanhGia>> GetAllAsync()
        {
            return await _context.DanhGias
                .Include(dg => dg.NguoiDung)
                .Include(dg => dg.Homestay)
                .Include(dg => dg.PhieuDatPhong)
                .ToListAsync();
        }

        public async Task<DanhGia> GetByIdAsync(string idDG)
        {
            return await _context.DanhGias
                .Include(dg => dg.NguoiDung)
                .Include(dg => dg.Homestay)
                .Include(dg => dg.PhieuDatPhong)
                .FirstOrDefaultAsync(dg => dg.ID_DG == idDG);
        }

        public async Task<DanhGia> GetByMapdpAsync(string maPdp)
        {
            return await _context.DanhGias
                .Include(dg => dg.NguoiDung)
                .Include(dg => dg.Homestay)
                .Include(dg => dg.PhieuDatPhong)
                .FirstOrDefaultAsync(dg => dg.Ma_PDPhong == maPdp);
        }

        public async Task AddAsync(DanhGia danhGia)
        {
            if (string.IsNullOrEmpty(danhGia.ID_DG))
            {
                danhGia.ID_DG = Guid.NewGuid().ToString(); 
            }
            await _context.DanhGias.AddAsync(danhGia);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(DanhGia danhGia)
        {
            _context.DanhGias.Update(danhGia);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(string idDG)
        {
            var danhGia = await _context.DanhGias
                .FirstOrDefaultAsync(dg => dg.ID_DG == idDG);
            if (danhGia != null)
            {
                _context.DanhGias.Remove(danhGia);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<DanhGia>> SearchAsync(
            string searchString,
            string idHomestay,
            DateTime? startDate,
            DateTime? endDate,
            short? minRating,
            short? maxRating)
        {
            var query = _context.DanhGias
                .Include(dg => dg.NguoiDung)
                .Include(dg => dg.Homestay)
                .Include(dg => dg.PhieuDatPhong)
                .AsQueryable();

            if (!string.IsNullOrEmpty(idHomestay))
            {
                query = query.Where(dg => dg.ID_Homestay == idHomestay);
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                var searchLower = searchString.ToLower();
                query = query.Where(dg => (dg.BinhLuan != null && dg.BinhLuan.ToLower().Contains(searchLower)) ||
                                         (dg.ID_DG != null && dg.ID_DG.ToLower().Contains(searchLower)));
            }

            if (startDate.HasValue)
            {
                query = query.Where(dg => dg.NgayDanhGia >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                query = query.Where(dg => dg.NgayDanhGia <= endDate.Value);
            }

            if (minRating.HasValue)
            {
                query = query.Where(dg => dg.Rating >= minRating.Value);
            }
            if (maxRating.HasValue)
            {
                query = query.Where(dg => dg.Rating <= maxRating.Value);
            }

            return await query.ToListAsync();
        }
        public async Task<IEnumerable<DanhGia>> SearchAsync(string searchString,List<string> homestayIds, DateTime? startDate,DateTime? endDate,short? minRating,short? maxRating)
        {
            var query = _context.DanhGias
                .Include(dg => dg.NguoiDung)
                .Include(dg => dg.Homestay)
                .Include(dg => dg.PhieuDatPhong)
                .AsQueryable();

            if (homestayIds != null && homestayIds.Any())
            {
                query = query.Where(dg => homestayIds.Contains(dg.ID_Homestay));
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                var searchLower = searchString.ToLower();
                query = query.Where(dg => (dg.BinhLuan != null && dg.BinhLuan.ToLower().Contains(searchLower)) ||
                                         (dg.ID_DG != null && dg.ID_DG.ToLower().Contains(searchLower)));
            }

            if (startDate.HasValue)
            {
                query = query.Where(dg => dg.NgayDanhGia >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                query = query.Where(dg => dg.NgayDanhGia <= endDate.Value);
            }

            if (minRating.HasValue)
            {
                query = query.Where(dg => dg.Rating >= minRating.Value);
            }
            if (maxRating.HasValue)
            {
                query = query.Where(dg => dg.Rating <= maxRating.Value);
            }

            return await query.ToListAsync();
        }
        public async Task<List<DanhGia>> GetTopTestimonialsFromDistinctAreasAsync(int count)
        {
            var query = _context.DanhGias
                .Include(dg => dg.NguoiDung)
                .Include(dg => dg.Homestay)
                .ThenInclude(h => h.KhuVuc)
                .Where(dg => dg.Homestay != null && dg.Homestay.KhuVuc != null)
                .OrderByDescending(dg => dg.Rating)
                .AsQueryable();

            var topTestimonials = await query
                .GroupBy(dg => dg.Homestay.Ma_KV)
                .Select(g => g.First())
                .Take(count)
                .ToListAsync();

            return topTestimonials;
        }

        public Task<bool> ExistsAsync(string userId, string maPdp)
        {
            return _context.DanhGias
                .AnyAsync(dg => dg.Ma_ND == userId && dg.Ma_PDPhong == maPdp);
        }
    }
}
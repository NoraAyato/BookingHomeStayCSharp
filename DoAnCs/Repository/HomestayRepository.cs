using DoAnCs.Areas.Admin.ModelsView;
using DoAnCs.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DoAnCs.Repository
{
    public class HomestayRepository : IHomestayRepository
    {
        private readonly ApplicationDbContext _context;

        public HomestayRepository(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<IEnumerable<Homestay>> GetAllAsync()
        {
            return await _context.Homestays
                .AsNoTracking()
                .ToListAsync();
        }

        public IQueryable<Homestay> GetAllQueryable()
        {
            return _context.Homestays
                .Include(h => h.NguoiDung)
                .Include(h => h.KhuVuc)
                .Include(h => h.Phongs)
                .AsQueryable();
        }

        public async Task<IEnumerable<Homestay>> GetPaginatedAsync(int pageNumber, int pageSize, string searchString = null, string locationFilter = null, string statusFilter = null, string sortOrder = null)
        {
            var query = _context.Homestays.AsQueryable(); 

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(h => h.Ten_Homestay.Contains(searchString) || h.DiaChi.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(locationFilter) && locationFilter != "all")
            {
                query = query.Where(h => h.Ma_KV == locationFilter);
            }

            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "all")
            {
                query = query.Where(h => h.TrangThai == statusFilter);
            }

            query = sortOrder switch
            {
                "name_desc" => query.OrderByDescending(h => h.Ten_Homestay),
                "Price" => query.OrderBy(h => h.PricePerNight),
                "price_desc" => query.OrderByDescending(h => h.PricePerNight),
                "Date" => query.OrderBy(h => h.NgayTao),
                "date_desc" => query.OrderByDescending(h => h.NgayTao),
                "rating_desc" => query.OrderByDescending(h => h.Hang),
                "rating_asc" => query.OrderBy(h => h.Hang),
                _ => query.OrderBy(h => h.Ten_Homestay),
            };

            return await query
                .AsNoTracking()
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> CountAsync(string searchString = null, string locationFilter = null, string statusFilter = null)
        {
            var query = _context.Homestays.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(h => h.Ten_Homestay.Contains(searchString) || h.DiaChi.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(locationFilter) && locationFilter != "all")
            {
                query = query.Where(h => h.Ma_KV == locationFilter);
            }

            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "all")
            {
                query = query.Where(h => h.TrangThai == statusFilter);
            }

            return await query.CountAsync();
        }

        public async Task<Homestay> GetByIdAsync(string id)
        {
            return await _context.Homestays
                .FirstOrDefaultAsync(h => h.ID_Homestay == id);
        }
        //Explicit Loading cho những truy vấn riêng lẻ
        public async Task<Homestay> GetByIdWithDetailsAsync(string id)
        {
            var homestay = await _context.Homestays
                .FirstOrDefaultAsync(h => h.ID_Homestay == id);

            if (homestay != null)
            {
                await _context.Entry(homestay)
                    .Reference(h => h.KhuVuc)
                    .LoadAsync();
                await _context.Entry(homestay)
                    .Reference(h => h.NguoiDung)
                    .LoadAsync();
            }

            return homestay;
        }

        public async Task AddAsync(Homestay homestay)
        {
            if (homestay == null)
            {
                throw new ArgumentNullException(nameof(homestay));
            }

            homestay.NgayTao = DateTime.Now;
            await _context.Homestays.AddAsync(homestay);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Homestay homestay)
        {
            if (homestay == null)
            {
                throw new ArgumentNullException(nameof(homestay));
            }

            _context.Homestays.Update(homestay);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(string id)
        {
            var homestay = await _context.Homestays.FindAsync(id);
            if (homestay != null)
            {
                _context.Homestays.Remove(homestay);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(string id)
        {
            return await _context.Homestays.AnyAsync(h => h.ID_Homestay == id);
        }

        public async Task<int> CountActiveHomestaysAsync()
        {
            return await _context.Homestays
                .Where(h => h.TrangThai == "Hoạt động")
                .CountAsync();
        }

        public async Task<IEnumerable<Homestay>> GetTopRatedHomestaysAsync(int count)
        {
            return await _context.Homestays.Where(h => h.TrangThai == "Hoạt động")
                .Include(h => h.KhuVuc)
                .OrderByDescending(h => h.DanhGias)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<Homestay>> GetHomestaysByOwnerAsync(string ownerId)
        {
            return await _context.Homestays
                .Where(h => h.Ma_ND == ownerId)
                .Include(h => h.KhuVuc)
                .ToListAsync();
        }
        public async Task<IEnumerable<Homestay>> GetPaginatedByOwnerAsync(
         string ownerId, int pageNumber, int pageSize,
         string searchString = null, string locationFilter = null,
         string statusFilter = null, string sortOrder = null)
        {
            if (string.IsNullOrEmpty(ownerId))
            {
                throw new ArgumentNullException(nameof(ownerId));
            }

            var query = _context.Homestays
                .Where(h => h.Ma_ND == ownerId)
                .Include(h => h.KhuVuc)
                .Include(h => h.Phongs)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(h => h.Ten_Homestay.Contains(searchString) || h.DiaChi.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(locationFilter) && locationFilter != "all")
            {
                query = query.Where(h => h.Ma_KV == locationFilter);
            }

            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "all")
            {
                query = query.Where(h => h.TrangThai == statusFilter);
            }

            query = sortOrder switch
            {
                "name_desc" => query.OrderByDescending(h => h.Ten_Homestay),
                "Price" => query.OrderBy(h => h.PricePerNight),
                "price_desc" => query.OrderByDescending(h => h.PricePerNight),
                "Date" => query.OrderBy(h => h.NgayTao),
                "date_desc" => query.OrderByDescending(h => h.NgayTao),
                "rating_desc" => query.OrderByDescending(h => h.Hang),
                "rating_asc" => query.OrderBy(h => h.Hang),
                _ => query.OrderBy(h => h.Ten_Homestay),
            };

            return await query
                .AsNoTracking()
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> CountByOwnerAsync(
            string ownerId, string searchString = null,
            string locationFilter = null, string statusFilter = null)
        {
            if (string.IsNullOrEmpty(ownerId))
            {
                throw new ArgumentNullException(nameof(ownerId));
            }

            var query = _context.Homestays
                .Where(h => h.Ma_ND == ownerId)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(h => h.Ten_Homestay.Contains(searchString) || h.DiaChi.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(locationFilter) && locationFilter != "all")
            {
                query = query.Where(h => h.Ma_KV == locationFilter);
            }

            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "all")
            {
                query = query.Where(h => h.TrangThai == statusFilter);
            }

            return await query.CountAsync();
        }
        public async Task<List<PopularHomestay>> GetPopularHomestaysAsync()
        {
            return await _context.Homestays.Where(h => h.TrangThai == "Hoạt động")
                .Select(h => new PopularHomestay
                {
                    Id = h.ID_Homestay,
                    Name = h.Ten_Homestay,
                    Image = h.HinhAnh ?? "",
                    Rank = h.Hang,
                    Address = h.DiaChi,
                    OriginalPrice = h.PricePerNight,
                    BookingCount = h.Phongs.SelectMany(p => p.ChiTietDatPhongs).Count()
                })
                .OrderByDescending(h => h.BookingCount)
                .Take(5)
                .ToListAsync();
        }
    }
}
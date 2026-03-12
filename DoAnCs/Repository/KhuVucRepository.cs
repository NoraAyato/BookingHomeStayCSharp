using DoAnCs.Areas.Admin.ModelsView;
using Microsoft.EntityFrameworkCore;

namespace DoAnCs.Repository
{
    public class KhuVucRepository : IKhuVucRepository
    {
        private readonly ApplicationDbContext _context;

        public KhuVucRepository(ApplicationDbContext context)
        {
            _context = context;
        }
       
        public async Task<KhuVuc> GetByNameAsync(string name)
        {
            return await _context.KhuVucs
                .Where(k => k.Ten_KV.ToLower() == name.ToLower())
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<object>> SearchByNameAsync(string term)
        {
            return await _context.KhuVucs
                .Where(k => k.Ten_KV.ToLower().Contains(term.ToLower()))
                .Select(k => new { k.Ma_KV, k.Ten_KV })
                .Take(10)
                .AsNoTracking()
                .ToListAsync();
        }
        public async Task<IEnumerable<KhuVuc>> GetAllAsync()
        {
            return await _context.KhuVucs
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<KhuVuc> GetByIdAsync(string id)
        {
            return await _context.KhuVucs
                .AsNoTracking()
                .FirstOrDefaultAsync(k => k.Ma_KV == id);
        }

        public async Task AddAsync(KhuVuc khuVuc)
        {
            if (khuVuc == null)
            {
                throw new ArgumentNullException(nameof(khuVuc));
            }

            await _context.KhuVucs.AddAsync(khuVuc);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(KhuVuc khuVuc)
        {
            if (khuVuc == null)
            {
                throw new ArgumentNullException(nameof(khuVuc));
            }

            _context.KhuVucs.Update(khuVuc);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(string id)
        {
            var khuVuc = await _context.KhuVucs.FindAsync(id);
            if (khuVuc != null)
            {
                _context.KhuVucs.Remove(khuVuc);
                await _context.SaveChangesAsync();
            }
        }
        public async Task<List<PopularArea>> GetPopularAreasAsync()
        {
            return await _context.ChiTietDatPhongs
                .GroupBy(ct => ct.Phong.Homestay.Ma_KV)
                .Select(g => new PopularArea
                {
                    Ma_KV = g.Key,
                    Ten_KV = g.First().Phong.Homestay.KhuVuc.Ten_KV,
                    BookingCount = g.Count()
                })
                .OrderByDescending(a => a.BookingCount)
                .Take(5)
                .ToListAsync();
        }
        public async Task<bool> ExistsAsync(string id)
        {
            return await _context.KhuVucs.AnyAsync(k => k.Ma_KV == id);
        }

        public async Task<bool> HasHomestaysAsync(string id)
        {
            return await _context.Homestays.AnyAsync(h => h.Ma_KV == id);
        }
    }
}

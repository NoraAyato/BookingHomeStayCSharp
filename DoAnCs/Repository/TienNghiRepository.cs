using DoAnCs.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DoAnCs.Repository
{
    public class TienNghiRepository : ITienNghiRepository
    {
        private readonly ApplicationDbContext _context;

        public TienNghiRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<TienNghi>> GetAllAsync()
        {
            return await _context.TienNghis.ToListAsync();
        }
        public async Task<bool> ExistsAsync(string id)
        {
            return await _context.TienNghis.AnyAsync(h => h.Ma_TienNghi == id);
        }
        public async Task<TienNghi> GetByIdAsync(string id)
        {
            return await _context.TienNghis.FindAsync(id);
        }

        public async Task AddAsync(TienNghi tienNghi)
        {
            await _context.TienNghis.AddAsync(tienNghi);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(TienNghi tienNghi)
        {
            _context.TienNghis.Update(tienNghi);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(string id)
        {
            var tienNghi = await _context.TienNghis.FindAsync(id);
            if (tienNghi != null)
            {
                _context.TienNghis.Remove(tienNghi);
                await _context.SaveChangesAsync();
            }
        }
        public async Task<IEnumerable<object>> GetAllAsValueTextAsync()
        {
            return await _context.TienNghis
                .Select(t => new { Value = t.Ma_TienNghi, Text = t.TenTienNghi })
                .ToListAsync();
        }
        public Task<int> CountAsync()
        {
            return _context.TienNghis.CountAsync();
        }
    }
}
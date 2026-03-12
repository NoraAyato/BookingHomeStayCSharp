using DoAnCs.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DoAnCs.Repository
{
    public class HuyPhongRepository : IHuyPhongRepository
    {
        private readonly ApplicationDbContext _context;

        public HuyPhongRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IEnumerable<PhieuHuyPhong>> GetAllAsync()
        {
            try
            {
                return await _context.PhieuHuyPhongs
                    .Include(hp => hp.PhieuDatPhong)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
               throw new Exception("Error retrieving cancellation records: " + ex.Message, ex);
            }
        }

        public async Task AddAsync(PhieuHuyPhong phieuHuyPhong)
        {
            await _context.PhieuHuyPhongs.AddAsync(phieuHuyPhong);
            await _context.SaveChangesAsync();
        }

        public async Task<PhieuHuyPhong> GetByIdAsync(string maPHP)
        {
            return await _context.PhieuHuyPhongs
                .Include(hp => hp.PhieuDatPhong)
                .FirstOrDefaultAsync(hp => hp.MaPHP == maPHP);
        }

        public async Task<PhieuHuyPhong> GetByMaPDPhongAsync(string maPDPhong)
        {
            return await _context.PhieuHuyPhongs
                .Include(hp => hp.PhieuDatPhong)
                .FirstOrDefaultAsync(hp => hp.Ma_PDPhong == maPDPhong);
        }

        public async Task<bool> HasCancellationAsync(string maPDPhong)
        {
            return await _context.PhieuHuyPhongs
                .AnyAsync(hp => hp.Ma_PDPhong == maPDPhong);
        }

        public async Task UpdateAsync(PhieuHuyPhong phieuHuyPhong)
        {
            _context.PhieuHuyPhongs.Update(phieuHuyPhong);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<PhieuHuyPhong>> GetByUserIdAsync(string userId)
        {
            return await _context.PhieuHuyPhongs
                .Include(hp => hp.PhieuDatPhong)
                .Where(hp => hp.PhieuDatPhong.Ma_ND == userId)
                .ToListAsync();
        }
    }
}
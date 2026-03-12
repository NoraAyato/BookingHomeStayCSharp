using Microsoft.EntityFrameworkCore;

namespace DoAnCs.Repository
{
    public class ThanhToanRepository : IThanhToanRepository
    {
        private readonly ApplicationDbContext _context;
        public ThanhToanRepository(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<IEnumerable<ThanhToan>> GetAllAsync()
        {
            return await _context.ThanhToans
                .Include(tt => tt.HoaDon)
                .ThenInclude(hd => hd.NguoiDung)
                .ToListAsync();
        }

        public async Task<ThanhToan> GetByIdAsync(string maTT)
        {
            return await _context.ThanhToans
                .Include(tt => tt.HoaDon)
                .ThenInclude(hd => hd.NguoiDung)
                .FirstOrDefaultAsync(tt => tt.MaTT == maTT);
        }
        public async Task<ThanhToan> GetByMaHDAsync(string maHD)
        {
            return await _context.ThanhToans
                .Include(tt => tt.HoaDon)
                .ThenInclude(hd => hd.NguoiDung)
                .FirstOrDefaultAsync(tt => tt.MaHD == maHD);
        }
        public async Task AddAsync(ThanhToan thanhToan)
        {
            
            await _context.ThanhToans.AddAsync(thanhToan);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(ThanhToan thanhToan)
        {
            if (thanhToan == null)
                throw new ArgumentNullException(nameof(thanhToan));

            _context.ThanhToans.Update(thanhToan);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(string maTT)
        {
            var thanhToan = await _context.ThanhToans.FindAsync(maTT);
            if (thanhToan == null)
                throw new Exception("Không tìm thấy thanh toán để xóa");

            _context.ThanhToans.Remove(thanhToan);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<ThanhToan>> GetByHoaDonAsync(string maHD)
        {
            return await _context.ThanhToans
                .Where(tt => tt.MaHD == maHD)
                .Include(tt => tt.HoaDon)
                .ToListAsync();
        }

        public async Task DeleteByMaHDAsync(string Ma_HD)
        {
            var thanhToan = await _context.ThanhToans
                .FirstOrDefaultAsync(tt => tt.MaHD == Ma_HD);

            if (thanhToan != null)
            {
                _context.ThanhToans.Remove(thanhToan);
                await _context.SaveChangesAsync();
            }
            else
            {
                throw new Exception("Không tìm thấy thanh toán để xóa");
            }
        }
    }
}

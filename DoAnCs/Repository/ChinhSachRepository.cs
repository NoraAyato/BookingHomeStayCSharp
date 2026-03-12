using DoAnCs.Models;
using Microsoft.EntityFrameworkCore;

namespace DoAnCs.Repository
{
    public class ChinhSachRepository : IChinhSachRepository
    {
        private readonly ApplicationDbContext _context;

        public ChinhSachRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ChinhSach> GetByHomestayIdAsync(string homestayId)
        {
            return await _context.ChinhSachs
                .FirstOrDefaultAsync(cs => cs.ID_Homestay == homestayId);
        }

        public async Task AddAsync(ChinhSach chinhSach)
        {
            _context.ChinhSachs.Add(chinhSach);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(ChinhSach chinhSach)
        {
            _context.ChinhSachs.Update(chinhSach);
            await _context.SaveChangesAsync();
        }
    }
}
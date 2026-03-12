using DoAnCs.Models;

namespace DoAnCs.Repository
{
    public interface IChinhSachRepository
    {
        Task<ChinhSach> GetByHomestayIdAsync(string homestayId);
        Task AddAsync(ChinhSach chinhSach);
        Task UpdateAsync(ChinhSach chinhSach);
    }
}
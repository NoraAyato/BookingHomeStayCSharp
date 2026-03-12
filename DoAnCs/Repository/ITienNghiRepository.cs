using DoAnCs.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DoAnCs.Repository
{
    public interface ITienNghiRepository
    {
        Task<IEnumerable<TienNghi>> GetAllAsync();
        Task<TienNghi> GetByIdAsync(string id);
        Task AddAsync(TienNghi tienNghi);
        Task UpdateAsync(TienNghi tienNghi);
        Task DeleteAsync(string id);
        Task<int> CountAsync();
        Task<bool> ExistsAsync(string id);
        Task<IEnumerable<object>> GetAllAsValueTextAsync();
    }
}
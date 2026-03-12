using DoAnCs.Areas.Admin.ModelsView;
using DoAnCs.Models;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DoAnCs.Repository
{
    public interface INewsRepository
    { // Quản lý ChuDe
        Task<IEnumerable<ChuDe>> GetAllChuDeAsync();
        IQueryable<TinTuc> GetTinTucQueryable();
        Task<ChuDe> GetChuDeByIdAsync(string idChuDe);
        Task AddChuDeAsync(ChuDe chuDe); 
        Task UpdateChuDeAsync(ChuDe chuDe);
        Task DeleteChuDeAsync(string idChuDe);

        // Quản lý TinTuc
        Task<IEnumerable<TinTuc>> GetAllTinTucAsync();
        Task<TinTuc> GetTinTucByIdAsync(string maTinTuc);
        Task AddTinTucAsync(TinTuc tinTuc);
        Task UpdateTinTucAsync(TinTuc tinTuc);
        Task DeleteTinTucAsync(string maTinTuc);

        // Quản lý BinhLuan
        Task AddBinhLuanAsync(BinhLuan binhLuan);
        Task<IEnumerable<BinhLuan>> GetBinhLuansByTinTucAsync(string maTinTuc);
        Task<BinhLuan> GetBinhLuanByIdAsync(int maBinhLuan);
        Task DeleteBinhLuanAsync(int maBinhLuan);
        Task<int> CountAllTinTucAsync();

        Task<List<PopularNews>> GetPopularNewsAsync();
    }

}
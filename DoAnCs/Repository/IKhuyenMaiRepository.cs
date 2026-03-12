using DoAnCs.Models;
using DoAnCs.Models.ViewModels;

namespace DoAnCs.Repository
{
    public interface IKhuyenMaiRepository
    {
        IQueryable<KhuyenMai> GetAllQueryable();
        Task<KhuyenMai> GetByIdAsync(string maKM);
        Task AddAsync(KhuyenMai khuyenMai);
        Task UpdateAsync(KhuyenMai khuyenMai);
        Task DeleteAsync(string maKM);
        Task<List<KhuyenMai>> GetActivePromotionsAsync();
        Task<List<KhuyenMai>> GetAvailableKhuyenMaiAsync(string homestayId, List<string> roomIds, string userId, int numberOfNights);
        //// Quản lý ApDungKM
        Task<int> CountApDungKmAsync(string maKm);
        Task AddApDungKMAsync(ApDungKM apDungKM);
        Task DeleteApDungKMAsync(string maHD, string maKM);
        Task<int> GetHighestPromotionByHomestay(string homestayId);
        Task<List<KhuyenMaiViewModel>> GetTop2KhuyenMaiAsync();
    }
}

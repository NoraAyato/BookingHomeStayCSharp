using DoAnCs.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DoAnCs.Repository
{
    public interface IPhongRepository
    {
        // Lấy tất cả phòng
        Task<IEnumerable<Phong>> GetAllAsync();

        // Lấy phòng theo ID
        Task<Phong> GetByIdAsync(string maPhong);
        Task<List<Phong>> GetByIdsAsync(IEnumerable<string> maPhongs);
        // Lấy danh sách phòng theo homestay
        Task<IEnumerable<Phong>> GetByHomestayAsync(string idHomestay);

        // Thêm phòng mới
        Task AddAsync(Phong phong);

        // Cập nhật phòng
        Task UpdateAsync(Phong phong);

        // Xóa phòng
        Task DeleteAsync(string maPhong);

        // Lấy danh sách loại phòng
        Task<IEnumerable<LoaiPhong>> GetLoaiPhongsAsync();

        // Lấy chi tiết phòng theo mã phòng
        Task<IEnumerable<ChiTietPhong>> GetChiTietPhongsAsync(string maPhong);

        // Lấy hình ảnh phòng theo mã phòng
        Task<IEnumerable<HinhAnhPhong>> GetHinhAnhPhongsAsync(string maPhong);
        Task<List<Phong>> GetByHostIdAsync(string hostId);
        Task<bool> ExistsLoaiPhongAsync(string id);
        Task<bool> ExistsPhongAsync(string id);
        Task<IEnumerable<(DateTime NgayDen, DateTime NgayDi)>> GetBookedDateRangesAsync(string maPhong);
    }
}
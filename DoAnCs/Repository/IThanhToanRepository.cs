namespace DoAnCs.Repository
{
    public interface IThanhToanRepository
    {
        Task<IEnumerable<ThanhToan>> GetAllAsync();
        Task<ThanhToan> GetByIdAsync(string maTT);
        Task AddAsync(ThanhToan thanhToan);
        Task UpdateAsync(ThanhToan thanhToan);
        Task DeleteAsync(string maTT);
        Task<IEnumerable<ThanhToan>> GetByHoaDonAsync(string maHD);
        Task<ThanhToan> GetByMaHDAsync(string maHD);
        Task DeleteByMaHDAsync(string Ma_HD);
    }
}

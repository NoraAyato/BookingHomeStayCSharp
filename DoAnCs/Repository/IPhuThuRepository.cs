namespace DoAnCs.Repository
{
    public interface IPhuThuRepository
    {
        // PhieuPhuThu
        IQueryable<PhieuPhuThu> GetAllAsync();
        Task<PhieuPhuThu> GetByIdAsync(string maPhieuPT);
        Task AddAsync(PhieuPhuThu phieuPhuThu);
        Task UpdateAsync(PhieuPhuThu phieuPhuThu);
        Task DeleteAsync(string maPhieuPT);
        Task<List<ApDungPhuThu>> GetApDungPhuThuByLoaiPhongAsync(string idLoai, DateTime startDate, DateTime endDate);
        Task<decimal> CalculatePhuThuAsync(string roomTypeId, DateTime checkInDate, DateTime checkOutDate, decimal donGia);

        // ApDungPhuThu
        Task<IEnumerable<ApDungPhuThu>> GetByPhieuPhuThuAsync(string maPhieuPT);
        Task AddRangeAsync(IEnumerable<ApDungPhuThu> apDungPhuThus);
        Task DeleteByPhieuPhuThuAsync(string maPhieuPT);

    }
}

using DoAnCs.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DoAnCs.Repository
{
    public class PhuThuRepository : IPhuThuRepository
    {
        private readonly ApplicationDbContext _context;

        public PhuThuRepository(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // PhieuPhuThu
        public IQueryable<PhieuPhuThu> GetAllAsync()
        {
            return _context.PhieuPhuThus
                .Include(pt => pt.ApDungPhuThus)
                .ThenInclude(ap => ap.LoaiPhong)
                .AsQueryable();
        }
        public async Task<decimal> CalculatePhuThuAsync(string roomTypeId, DateTime checkInDate, DateTime checkOutDate, decimal donGia)
        {
            decimal totalPhuThu = 0;
            var apDungPhuThus = await GetApDungPhuThuByLoaiPhongAsync(roomTypeId, checkInDate, checkOutDate);
            foreach (var apDung in apDungPhuThus)
            {
                if (apDung.NgayApDung >= checkInDate && apDung.NgayApDung <= checkOutDate)
                {
                    totalPhuThu += (apDung.PhieuPhuThu.PhiPhuThu * donGia);
                }
            }
            return totalPhuThu;
        }
        public async Task<PhieuPhuThu> GetByIdAsync(string maPhieuPT)
        {
            return await _context.PhieuPhuThus
                .Include(pt => pt.ApDungPhuThus)
                .ThenInclude(ap => ap.LoaiPhong)
                .FirstOrDefaultAsync(pt => pt.Ma_PhieuPT == maPhieuPT);
        }
        public async Task<List<ApDungPhuThu>> GetApDungPhuThuByLoaiPhongAsync(string idLoai, DateTime startDate, DateTime endDate)
        {
            return await _context.ApDungPhuThus
                .Include(ap => ap.PhieuPhuThu)
                .Where(ap => ap.ID_Loai == idLoai &&
                             ap.NgayApDung >= startDate &&
                             ap.NgayApDung <= endDate)
                .ToListAsync();
        }
        public async Task AddAsync(PhieuPhuThu phieuPhuThu)
        {
            await _context.PhieuPhuThus.AddAsync(phieuPhuThu);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(PhieuPhuThu phieuPhuThu)
        {
            var existing = await _context.PhieuPhuThus
                .Include(pt => pt.ApDungPhuThus)
                .FirstOrDefaultAsync(pt => pt.Ma_PhieuPT == phieuPhuThu.Ma_PhieuPT);

            if (existing == null)
                throw new Exception("Không tìm thấy phiếu phụ thu");

            _context.Entry(existing).CurrentValues.SetValues(phieuPhuThu);

            _context.ApDungPhuThus.RemoveRange(existing.ApDungPhuThus);
            if (phieuPhuThu.ApDungPhuThus != null)
            {
                _context.ApDungPhuThus.AddRange(phieuPhuThu.ApDungPhuThus);
            }

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(string maPhieuPT)
        {
            var phieuPhuThu = await _context.PhieuPhuThus
                .Include(pt => pt.ApDungPhuThus)
                .FirstOrDefaultAsync(pt => pt.Ma_PhieuPT == maPhieuPT);

            if (phieuPhuThu == null)
                throw new Exception("Không tìm thấy phiếu phụ thu");

            _context.ApDungPhuThus.RemoveRange(phieuPhuThu.ApDungPhuThus);
            _context.PhieuPhuThus.Remove(phieuPhuThu);
            await _context.SaveChangesAsync();
        }

        // ApDungPhuThu
        public async Task<IEnumerable<ApDungPhuThu>> GetByPhieuPhuThuAsync(string maPhieuPT)
        {
            return await _context.ApDungPhuThus
                .Include(ap => ap.LoaiPhong)
                .Where(ap => ap.Ma_PhieuPT == maPhieuPT)
                .ToListAsync();
        }

        public async Task AddRangeAsync(IEnumerable<ApDungPhuThu> apDungPhuThus)
        {
            await _context.ApDungPhuThus.AddRangeAsync(apDungPhuThus);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteByPhieuPhuThuAsync(string maPhieuPT)
        {
            var apDungPhuThus = await _context.ApDungPhuThus
                .Where(ap => ap.Ma_PhieuPT == maPhieuPT)
                .ToListAsync();
            _context.ApDungPhuThus.RemoveRange(apDungPhuThus);
            await _context.SaveChangesAsync();
        }
    }
}
using DoAnCs.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DoAnCs.Repository
{
    public class HoaDonRepository : IHoaDonRepository
    {
        private readonly ApplicationDbContext _context;
        public HoaDonRepository(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<IEnumerable<HoaDon>> GetAllAsync()
        {
            return await _context.HoaDons
                .Include(hd => hd.NguoiDung)
                .Include(hd => hd.ChiTietHoaDons)
                .ThenInclude(ct => ct.PhieuDatPhong)
                .Include(hd => hd.ApDungKMs)
                .ThenInclude(ad => ad.KhuyenMai)
                .Include(hd => hd.ThanhToans)
                .ToListAsync();
        }

        public async Task<HoaDon> GetByIdAsync(string maHD)
        {
            return await _context.HoaDons
                .Include(hd => hd.NguoiDung)
                .Include(hd => hd.ChiTietHoaDons)
                .ThenInclude(ct => ct.PhieuDatPhong)
                .Include(hd => hd.ApDungKMs)
                .ThenInclude(ad => ad.KhuyenMai)
                .Include(hd => hd.ThanhToans)
                .FirstOrDefaultAsync(hd => hd.Ma_HD == maHD);
        }

        public async Task AddAsync(HoaDon hoaDon)
        {
            if (hoaDon == null)
                throw new ArgumentNullException(nameof(hoaDon));

            hoaDon.NgayLap = DateTime.Now;
            await _context.HoaDons.AddAsync(hoaDon);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(HoaDon hoaDon)
        {
            if (hoaDon == null)
                throw new ArgumentNullException(nameof(hoaDon));

            var existingHoaDon = await _context.HoaDons
                .Include(hd => hd.ChiTietHoaDons)
                .Include(hd => hd.ApDungKMs)
                .FirstOrDefaultAsync(hd => hd.Ma_HD == hoaDon.Ma_HD);

            if (existingHoaDon == null)
                throw new Exception("Không tìm thấy hóa đơn để cập nhật");

            _context.Entry(existingHoaDon).CurrentValues.SetValues(hoaDon);

            _context.ChiTietHoaDons.RemoveRange(existingHoaDon.ChiTietHoaDons);
            if (hoaDon.ChiTietHoaDons != null)
            {
                _context.ChiTietHoaDons.AddRange(hoaDon.ChiTietHoaDons);
            }

            _context.ApDungKMs.RemoveRange(existingHoaDon.ApDungKMs);
            if (hoaDon.ApDungKMs != null)
            {
                _context.ApDungKMs.AddRange(hoaDon.ApDungKMs);
            }

            await _context.SaveChangesAsync();
        }

        public async Task UpdateStatusAsync(string maHD, string trangThai)
        {
            try
            {
                var existingHoaDon = await _context.HoaDons
                    .FirstOrDefaultAsync(hd => hd.Ma_HD == maHD);

                if (existingHoaDon == null)
                {
                    throw new Exception("Không tìm thấy hóa đơn để cập nhật");
                }

                existingHoaDon.TrangThai = trangThai;
                int rowsAffected = await _context.SaveChangesAsync();
            }
            catch (DbUpdateException dbEx)
            {
                if (dbEx.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {dbEx.InnerException.Message}");
                }
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateStatusAsync: {ex.Message}");
                throw;
            }
        }

        public async Task DeleteAsync(string maHD)
        {
            var hoaDon = await _context.HoaDons
                .Include(hd => hd.ChiTietHoaDons)
                .Include(hd => hd.ApDungKMs)
                .Include(hd => hd.ThanhToans)
                .FirstOrDefaultAsync(hd => hd.Ma_HD == maHD);

            if (hoaDon == null)
                throw new Exception("Không tìm thấy hóa đơn để xóa");

           
            _context.ChiTietHoaDons.RemoveRange(hoaDon.ChiTietHoaDons);
            _context.ThanhToans.RemoveRange(hoaDon.ThanhToans);
            _context.ApDungKMs.RemoveRange(hoaDon.ApDungKMs);
            _context.HoaDons.Remove(hoaDon);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<HoaDon>> GetByUserAsync(string maND)
        {
            return await _context.HoaDons
                .Where(hd => hd.Ma_ND == maND)
                .Include(hd => hd.ChiTietHoaDons)
                .ThenInclude(ct => ct.PhieuDatPhong)
                .Include(hd => hd.ApDungKMs)
                .ThenInclude(ad => ad.KhuyenMai)
                .Include(hd => hd.ThanhToans)
                .ToListAsync();
        }

        public async Task<bool> HasUnpaidInvoiceAsync(string maPDPhong)
        {
            return await _context.ChiTietHoaDons
                .AnyAsync(ct => ct.Ma_PDPhong == maPDPhong && ct.HoaDon.TrangThai != "Đã thanh toán");
        }

        public async Task<HoaDon> GetUnpaidInvoiceAsync(string maPDPhong)
        {
            return await _context.HoaDons
                .Include(hd => hd.ChiTietHoaDons)
                .Include(hd => hd.ApDungKMs)
                .ThenInclude(ad => ad.KhuyenMai)
                .Include(hd => hd.ThanhToans)
                .FirstOrDefaultAsync(hd => hd.ChiTietHoaDons.Any(ct => ct.Ma_PDPhong == maPDPhong) && hd.TrangThai != "Đã thanh toán");
        }

        public async Task<IEnumerable<ChiTietHoaDon>> GetByHoaDonAsync(string maHD)
        {
            return await _context.ChiTietHoaDons
                .Where(ct => ct.Ma_HD == maHD)
                .Include(ct => ct.PhieuDatPhong)
                .ToListAsync();
        }

        public async Task<HoaDon> GetByPhieuDatPhongAsync(string maPDPhong)
        {
            return await _context.HoaDons
                .Include(hd => hd.ChiTietHoaDons)
                .Include(hd => hd.ApDungKMs)
                .ThenInclude(ad => ad.KhuyenMai)
                .Include(hd => hd.ThanhToans)
                .FirstOrDefaultAsync(hd => hd.ChiTietHoaDons.Any(ct => ct.Ma_PDPhong == maPDPhong));
        }
        public async Task<List<HoaDon>> GetByHostIdAsync(string hostId)
        {
            return await _context.HoaDons
                .Include(h => h.NguoiDung)
                .Include(h => h.ChiTietHoaDons)
                    .ThenInclude(ct => ct.PhieuDatPhong)
                        .ThenInclude(pdp => pdp.ChiTietDatPhongs)
                            .ThenInclude(ctdp => ctdp.Phong)
                                .ThenInclude(ph => ph.Homestay)
                                    .ThenInclude(hs => hs.KhuVuc)
                .Include(h => h.ThanhToans)
                .Where(h => h.ChiTietHoaDons.Any(ct =>
                    ct.PhieuDatPhong.ChiTietDatPhongs.Any(ctdp =>
                        ctdp.Phong.Homestay.Ma_ND == hostId)))
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
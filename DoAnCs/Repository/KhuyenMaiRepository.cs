using DoAnCs.Models;
using DoAnCs.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DoAnCs.Repository
{
    public class KhuyenMaiRepository : IKhuyenMaiRepository
    {
        private readonly ApplicationDbContext _context;

        public KhuyenMaiRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public IQueryable<KhuyenMai> GetAllQueryable()
        {
            return _context.KhuyenMais
                .Include(km => km.NguoiTao)
                .Include(km => km.KhuyenMaiPhongs)
                .AsQueryable();
        }

        public async Task<KhuyenMai> GetByIdAsync(string maKM)
        {
            return await _context.KhuyenMais
                .Include(km => km.NguoiTao)
                .Include(km => km.KhuyenMaiPhongs)
                .FirstOrDefaultAsync(km => km.Ma_KM == maKM);
        }

        public async Task AddAsync(KhuyenMai khuyenMai)
        {
            await _context.KhuyenMais.AddAsync(khuyenMai);
            await _context.SaveChangesAsync();
        }
        public async Task<List<KhuyenMai>> GetActivePromotionsAsync()
        {
            var now = DateTime.Now;
            return await _context.KhuyenMais
                .Where(km => km.TrangThai == "Đang áp dụng" &&
                             km.NgayBatDau <= now &&
                             km.HSD >= now &&
                             km.SoLuong > 0)
                .Include(km => km.KhuyenMaiPhongs)
                .ToListAsync();
        }
        public async Task UpdateAsync(KhuyenMai khuyenMai)
        {
            var existing = await _context.KhuyenMais
                .Include(km => km.KhuyenMaiPhongs)
                .FirstOrDefaultAsync(km => km.Ma_KM == khuyenMai.Ma_KM);

            if (existing == null)
            {
                throw new Exception("Khuyến mãi không tồn tại.");
            }

            // Cập nhật các thuộc tính cơ bản
            _context.Entry(existing).CurrentValues.SetValues(khuyenMai);

            // Cập nhật KhuyenMaiPhongs
            existing.KhuyenMaiPhongs.Clear();
            if (khuyenMai.KhuyenMaiPhongs != null)
            {
                foreach (var kmp in khuyenMai.KhuyenMaiPhongs)
                {
                    existing.KhuyenMaiPhongs.Add(new KhuyenMaiPhong
                    {
                        Ma_KM = khuyenMai.Ma_KM,
                        Ma_Phong = kmp.Ma_Phong,
                        ID_Homestay = kmp.ID_Homestay
                    });
                }
            }

            await _context.SaveChangesAsync();
        }
        public async Task<int> GetHighestPromotionByHomestay(string homestayId)
        {
            try
            {
                if (string.IsNullOrEmpty(homestayId))
                {
                    return 0;
                }

                var now = DateTime.Now;
                var khuyenMais = await _context.KhuyenMais
                    .Where(km => km.TrangThai == "Đang áp dụng" &&
                                 km.NgayBatDau <= now &&
                                 km.HSD >= now &&
                                 (km.SoLuong > 0 || km.SoLuong == 0))
                    .Include(km => km.KhuyenMaiPhongs)
                    .ToListAsync();

                // Lọc khuyến mãi hợp lệ
                var matchingKhuyenMais = khuyenMais
                    .Where(km => km.KhuyenMaiPhongs != null && km.KhuyenMaiPhongs.Any(kmp => kmp.ID_Homestay == homestayId))
                    .Where(km => km.ChietKhau != null)
                    .ToList();

                if (!matchingKhuyenMais.Any())
                {
                    return 0;
                }

                var maxChietKhau = matchingKhuyenMais.Max(km => km.ChietKhau);       
                return (int)maxChietKhau;
            }
            catch (Exception ex)
            {
                return 0; // Trả về 0 thay vì ném lỗi
            }
        }
        public async Task<List<KhuyenMai>> GetAvailableKhuyenMaiAsync(string homestayId, List<string> roomIds, string userId, int numberOfNights)
        {
            var now = DateTime.Now;
            var query = _context.KhuyenMais
                .Where(km => km.TrangThai == "Đang áp dụng" &&
                             km.NgayBatDau <= now &&
                             km.HSD >= now &&
                             (km.SoLuong > 0 || km.SoLuong == 0))
                .Include(km => km.KhuyenMaiPhongs)
                .AsQueryable();

            // Lọc khuyến mãi áp dụng cho homestay hoặc phòng
            var potentialKhuyenMais = await query
                .Where(km => 
                             km.ApDungChoTatCaPhong ||
                             km.KhuyenMaiPhongs.Any(kmp => roomIds.Contains(kmp.Ma_Phong)))
                .ToListAsync();

            var result = new List<KhuyenMai>();

            foreach (var km in potentialKhuyenMais)
            {
                bool isValid = true;

                // Kiểm tra SoDemToiThieu
                if (km.SoDemToiThieu.HasValue && numberOfNights < km.SoDemToiThieu.Value)
                {
                    isValid = false;
                }

                // Kiểm tra ChiApDungChoKhachMoi
                if (km.ChiApDungChoKhachMoi && !string.IsNullOrEmpty(userId))
                {
                    var hasPreviousBookings = await _context.PhieuDatPhongs
                        .AnyAsync(pdp => pdp.Ma_ND == userId && pdp.NgayLap < now);
                    if (hasPreviousBookings)
                    {
                        isValid = false;
                    }
                }

                // Kiểm tra số lượng sử dụng
                if (km.SoLuong > 0)
                {
                    var usedCount = await _context.ApDungKMs
                        .CountAsync(ad => ad.Ma_KM == km.Ma_KM);
                    if (usedCount >= km.SoLuong)
                    {
                        isValid = false;
                    }
                }

                if (isValid)
                {
                    result.Add(km);
                }
            }

            return result;
        }
        public async Task DeleteAsync(string maKM)
        {
            var apdungKMs = await _context.ApDungKMs
                .Where(ad => ad.Ma_KM == maKM)
                .ToListAsync();
            if (apdungKMs.Any())
            {
                throw new Exception("Khuyến mãi đã được sử dụng.");
            }
            var khuyenMai = await _context.KhuyenMais.FindAsync(maKM);
            if (khuyenMai == null)
            {
                throw new Exception("Khuyến mãi không tồn tại.");
            }
            _context.KhuyenMais.Remove(khuyenMai);
            await _context.SaveChangesAsync();
        }

        public Task<int> CountApDungKmAsync(string maKm)
        {
            return _context.ApDungKMs
                .CountAsync(ad => ad.Ma_KM == maKm);
        }
        public async Task<List<KhuyenMaiViewModel>> GetTop2KhuyenMaiAsync()
        {
            var now = DateTime.Now;
            return await _context.KhuyenMais
                .Where(km => km.TrangThai == "Đang áp dụng" && km.HSD >= now)
                .OrderByDescending(km => km.ChietKhau)
                .Take(2)
                .Select(km => new KhuyenMaiViewModel
                {
                    MaKM = km.Ma_KM,
                    TieuDe = km.NoiDung,
                    MoTa = $"Giảm {(km.LoaiChietKhau == "Percentage" ? $"{km.ChietKhau}%" : $"{km.ChietKhau} VNĐ")} cho đơn từ {km.SoDemToiThieu ?? 1} đêm trở lên",
                    MaGiamGia = km.Ma_KM,
                    HSD = km.HSD,
                    ThoiGianConLai = $"Còn {(km.HSD - now).Days} ngày",
                    AnhDaiDien = km.HinhAnh ?? "/img/promotions/Default-promotion.jpg"
                })
                .ToListAsync();
        }

        public Task AddApDungKMAsync(ApDungKM apDungKM)
        {
            _context.ApDungKMs.Add(apDungKM);
            return _context.SaveChangesAsync();
        }
        public async Task DeleteApDungKMAsync(string maHD, string maKM)
        {
            var apDungKM = await _context.ApDungKMs
                .FirstOrDefaultAsync(ad => ad.Ma_HD == maHD && ad.Ma_KM == maKM);

            if (apDungKM != null)
            {
                _context.ApDungKMs.Remove(apDungKM);
                await _context.SaveChangesAsync();
            }
        }
    }
}
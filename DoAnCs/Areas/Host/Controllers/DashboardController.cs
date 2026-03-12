using DoAnCs.Areas.Host.ModelsView;
using DoAnCs.Models;
using DoAnCs.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DoAnCs.Areas.Host.Controllers
{
    [Area("Host")]
    [Authorize(Roles = "Host")]
    public class DashboardController : Controller
    {
        private readonly IHoaDonRepository _hoaDonRepository;
        private readonly IPhieuDatPhongRepository _phieuDatPhongRepository;
        private readonly IHomestayRepository _homestayRepository;
        private readonly IServiceRepository _serviceRepository;
        private readonly ApplicationDbContext _context;

        public DashboardController(
            IHoaDonRepository hoaDonRepository,
            IPhieuDatPhongRepository phieuDatPhongRepository,
            IHomestayRepository homestayRepository,
            IServiceRepository serviceRepository,
            ApplicationDbContext context)
        {
            _hoaDonRepository = hoaDonRepository;
            _phieuDatPhongRepository = phieuDatPhongRepository;
            _homestayRepository = homestayRepository;
            _serviceRepository = serviceRepository;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetHostDashboardStats(int months = 12, string homestayId = null)
        {
            try
            {
                var hostId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(hostId))
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin Host" });
                }

                var stats = await GetHostDashboardStatsData(hostId, months, homestayId);
                return Json(new
                {
                    success = true,
                    data = new
                    {
                        stats = new
                        {
                            stats.TotalRevenue,
                            stats.TotalProfit,
                            stats.BookingCount,
                            stats.ConfirmedBookingCount,
                            stats.PendingBookingCount,
                            stats.RoomCount,
                            stats.AvailableRoomCount,
                            stats.OccupancyRate
                        },
                        monthlyRevenue = stats.MonthlyRevenue,
                        monthlyBookings = stats.MonthlyBookings,
                        homestayServices = stats.HomestayServices,
                        lastUpdated = DateTime.Now.ToString("o")
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi khi lấy thống kê: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRecentActivities(string homestayId = null)
        {
            try
            {
                var hostId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(hostId))
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin Host" });
                }

                var activities = await GetRecentActivitiesData(hostId, homestayId);
                return Json(new
                {
                    success = true,
                    data = new
                    {
                        recentBookings = activities.RecentBookings,
                        recentInvoices = activities.RecentInvoices,
                        recentActivities = activities.RecentActivities,
                        lastUpdated = DateTime.Now.ToString("o")
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi khi lấy hoạt động gần đây: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetHomestays()
        {
            try
            {
                var hostId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(hostId))
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin Host" });
                }

                var homestays = await _homestayRepository.GetHomestaysByOwnerAsync(hostId);
                var homestayList = homestays.Select(h => new
                {
                    id = h.ID_Homestay,
                    name = h.Ten_Homestay
                }).ToList();

                return Json(new { success = true, data = homestayList });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi khi lấy danh sách Homestay: {ex.Message}" });
            }
        }

        private async Task<HostDashboardStatsData> GetHostDashboardStatsData(string hostId, int months, string homestayId)
        {
            // Lấy danh sách Homestay của Host
            var homestays = await _homestayRepository.GetHomestaysByOwnerAsync(hostId);
            var homestayIds = string.IsNullOrEmpty(homestayId)
                ? homestays.Select(h => h.ID_Homestay).ToList()
                : homestays.Where(h => h.ID_Homestay == homestayId).Select(h => h.ID_Homestay).ToList();

            if (!homestayIds.Any())
            {
                return new HostDashboardStatsData();
            }

            // Lấy dữ liệu sử dụng các phương thức repository
            var invoices = await _hoaDonRepository.GetByHostIdAsync(hostId);
            invoices = invoices.Where(i => i.ChiTietHoaDons.Any(ct =>
                ct.PhieuDatPhong.ChiTietDatPhongs.Any(ctdp =>
                    homestayIds.Contains(ctdp.Phong.ID_Homestay)))).ToList();

            var bookings = await _phieuDatPhongRepository.GetByHostIdAsync(hostId);
            bookings = bookings.Where(b => b.ChiTietDatPhongs.Any(ct =>
                homestayIds.Contains(ct.Phong.ID_Homestay))).ToList();

            var rooms = await _context.Phongs
                .Where(p => homestayIds.Contains(p.ID_Homestay))
                .ToListAsync();

            // Thống kê hóa đơn
            var totalRevenue = invoices.Where(i => i.TrangThai == "Đã thanh toán").Sum(i => i.TongTien);
            var totalProfit = invoices.Where(i => i.TrangThai == "Đã thanh toán").Sum(i => i.TongTien * 0.85m); // Host nhận 85% sau phí 15%

            // Thống kê đặt phòng
            var bookingCount = bookings.Count();
            var confirmedBookingCount = bookings.Count(b => b.TrangThai == "Đã xác nhận");
            var pendingBookingCount = bookings.Count(b => b.TrangThai == "Chờ xác nhận");

            // Thống kê phòng
            var roomCount = rooms.Count;
            var bookedRoomIds = bookings
                .Where(b => b.TrangThai == "Đã xác nhận" && b.NgayLap <= DateTime.Now && b.ChiTietDatPhongs.Any(ct => ct.NgayDi >= DateTime.Now))
                .SelectMany(b => b.ChiTietDatPhongs.Select(ct => ct.Ma_Phong))
                .Distinct()
                .Count();
            var availableRoomCount = roomCount - bookedRoomIds;
            var occupancyRate = roomCount > 0 ? Math.Round((double)bookedRoomIds / roomCount * 100, 2) : 0;

            // Doanh thu và đặt phòng theo tháng
            var endDate = DateTime.Now;
            var startDate = endDate.AddMonths(-months + 1).AddDays(-endDate.Day + 1).Date;
            var allMonths = new List<MonthlyRevenue>();
            var allBookingMonths = new List<MonthlyBooking>();
            for (var date = startDate; date <= endDate; date = date.AddMonths(1))
            {
                allMonths.Add(new MonthlyRevenue { Month = $"{date.Month}/{date.Year}", Revenue = 0 });
                allBookingMonths.Add(new MonthlyBooking { Month = $"{date.Month}/{date.Year}", BookingCount = 0 });
            }

            var monthlyRevenueData = invoices
                .Where(i => i.TrangThai == "Đã thanh toán" && i.NgayLap >= startDate && i.NgayLap <= endDate)
                .GroupBy(i => new { i.NgayLap.Year, i.NgayLap.Month })
                .Select(g => new { MonthKey = $"{g.Key.Month}/{g.Key.Year}", Revenue = g.Sum(i => i.TongTien) })
                .ToList();

            foreach (var revenue in monthlyRevenueData)
            {
                var monthEntry = allMonths.FirstOrDefault(m => m.Month == revenue.MonthKey);
                if (monthEntry != null) monthEntry.Revenue = revenue.Revenue;
            }

            var monthlyBookingData = bookings
                .Where(b => b.NgayLap >= startDate && b.NgayLap <= endDate)
                .GroupBy(b => new { b.NgayLap.Year, b.NgayLap.Month })
                .Select(g => new { MonthKey = $"{g.Key.Month}/{g.Key.Year}", BookingCount = g.Count() })
                .ToList();

            foreach (var booking in monthlyBookingData)
            {
                var monthEntry = allBookingMonths.FirstOrDefault(m => m.Month == booking.MonthKey);
                if (monthEntry != null) monthEntry.BookingCount = booking.BookingCount;
            }

            // Dịch vụ homestay
            var homestayServices = (await _serviceRepository.GetByHostIdAsync(hostId))
                .Where(dv => homestayIds.Contains(dv.ID_Homestay))
                .GroupBy(dv => dv.Ten_DV)
                .Select(g => new HomestayService
                {
                    Ten_DV = g.Key,
                    BookingCount = g.Count()
                })
                .OrderByDescending(s => s.BookingCount)
                .Take(5)
                .ToList();

            return new HostDashboardStatsData
            {
                TotalRevenue = totalRevenue,
                TotalProfit = totalProfit,
                BookingCount = bookingCount,
                ConfirmedBookingCount = confirmedBookingCount,
                PendingBookingCount = pendingBookingCount,
                RoomCount = roomCount,
                AvailableRoomCount = availableRoomCount,
                OccupancyRate = occupancyRate,
                MonthlyRevenue = allMonths,
                MonthlyBookings = allBookingMonths,
                HomestayServices = homestayServices
            };
        }

        private async Task<HostRecentActivitiesData> GetRecentActivitiesData(string hostId, string homestayId)
        {
            var homestays = await _homestayRepository.GetHomestaysByOwnerAsync(hostId);
            var homestayIds = string.IsNullOrEmpty(homestayId)
                ? homestays.Select(h => h.ID_Homestay).ToList()
                : homestays.Where(h => h.ID_Homestay == homestayId).Select(h => h.ID_Homestay).ToList();

            var recentBookings = (await _phieuDatPhongRepository.GetByHostIdAsync(hostId))
                .Where(b => b.ChiTietDatPhongs.Any(ct => homestayIds.Contains(ct.Phong.ID_Homestay)))
                .OrderByDescending(b => b.NgayLap)
                .Take(5)
                .Select(b => new RecentBooking
                {
                    Id = b.Ma_PDPhong,
                    CustomerName = b.NguoiDung?.FullName ?? "Khách hàng",
                    Date = b.NgayLap,
                    Status = b.TrangThai,
                    RoomCount = b.ChiTietDatPhongs.Count
                })
                .ToList();

            var recentInvoices = (await _hoaDonRepository.GetByHostIdAsync(hostId))
                .Where(i => i.ChiTietHoaDons.Any(ct =>
                    ct.PhieuDatPhong.ChiTietDatPhongs.Any(ctdp =>
                        homestayIds.Contains(ctdp.Phong.ID_Homestay))))
                .OrderByDescending(i => i.NgayLap)
                .Take(5)
                .Select(i => new RecentInvoice
                {
                    Id = i.Ma_HD,
                    CustomerName = i.NguoiDung?.FullName ?? "Khách hàng",
                    Date = i.NgayLap,
                    Amount = i.TongTien,
                    Status = i.TrangThai
                })
                .ToList();

            var recentActivities = (await _phieuDatPhongRepository.GetByHostIdAsync(hostId))
                .Where(b => b.ChiTietDatPhongs.Any(ct => homestayIds.Contains(ct.Phong.ID_Homestay)))
                .OrderByDescending(b => b.NgayLap)
                .Take(5)
                .Select(b => new RecentActivity
                {
                    Description = $"Khách hàng {b.NguoiDung?.FullName ?? "Khách hàng"} đã đặt {b.ChiTietDatPhongs.Count} phòng",
                    Date = b.NgayLap
                })
                .ToList();

            return new HostRecentActivitiesData
            {
                RecentBookings = recentBookings,
                RecentInvoices = recentInvoices,
                RecentActivities = recentActivities
            };
        }
    }
}
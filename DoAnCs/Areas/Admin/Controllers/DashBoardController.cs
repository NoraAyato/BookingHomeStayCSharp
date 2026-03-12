using DoAnCs.Areas.Admin.ModelsView;
using DoAnCs.Models;
using DoAnCs.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DoAnCs.Areas.Admin.Controllers
{
    public class DashboardController : BaseController
    {
        private readonly IHoaDonRepository _hoaDonRepository;
        private readonly IPhieuDatPhongRepository _phieuDatPhongRepository;
        private readonly IHomestayRepository _homestayRepository;
        private readonly IUserRepository _userRepository;
        private readonly INewsRepository _newsRepository;
        private readonly IServiceRepository _serviceRepository;
        private readonly IKhuVucRepository _khuVucRepository;

        public DashboardController(
            IHoaDonRepository hoaDonRepository,
            IPhieuDatPhongRepository phieuDatPhongRepository,
            IHomestayRepository homestayRepository,
            IUserRepository userRepository,
            INewsRepository newsRepository,
            IServiceRepository serviceRepository,
            IKhuVucRepository khuVucRepository)
        {
            _hoaDonRepository = hoaDonRepository;
            _phieuDatPhongRepository = phieuDatPhongRepository;
            _homestayRepository = homestayRepository;
            _userRepository = userRepository;
            _newsRepository = newsRepository;
            _serviceRepository = serviceRepository;
            _khuVucRepository = khuVucRepository;
        }

        public async Task<IActionResult> Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardStats(int months = 12)
        {
            try
            {
                var stats = await GetDashboardStatsData(months);
                return Json(new
                {
                    success = true,
                    data = new
                    {
                        stats = new
                        {
                            stats.TotalRevenue,
                            stats.TotalProfit, // Thêm tổng lợi nhuận
                            stats.InvoiceCount,
                            stats.PaidInvoiceCount,
                            stats.UnpaidInvoiceCount,
                            stats.BookingCount,
                            stats.ConfirmedBookingCount,
                            stats.PendingBookingCount,
                            stats.UserCount,
                            stats.ActiveUserCount,
                            stats.InactiveUserCount,
                            stats.HomestayCount,
                            stats.ActiveHomestayCount,
                            stats.NewsCount,
                            stats.ServiceCount
                        },
                        monthlyRevenue = stats.MonthlyRevenue,
                        monthlyProfit = stats.MonthlyProfit, // Thêm lợi nhuận theo tháng
                        monthlyBookings = stats.MonthlyBookings,
                        popularHomestays = stats.PopularHomestays,
                        popularAreas = stats.PopularAreas,
                        popularNews = stats.PopularNews,
                        popularServices = stats.PopularServices,
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
        public async Task<IActionResult> GetRecentActivities()
        {
            try
            {
                var activities = await GetRecentActivitiesData();
                return Json(new
                {
                    success = true,
                    data = new
                    {
                        recentInvoices = activities.RecentInvoices,
                        recentBookings = activities.RecentBookings,
                        newUsers = activities.NewUsers,
                        lastUpdated = DateTime.Now.ToString("o")
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi khi lấy hoạt động gần đây: {ex.Message}" });
            }
        }

        private async Task<DashboardStatsData> GetDashboardStatsData(int months)
        {
            var invoices = await _hoaDonRepository.GetAllAsync();
            var bookings = await _phieuDatPhongRepository.GetAllAsync();
            var users = await _userRepository.GetAllAsync();
            var homestays = await _homestayRepository.GetAllAsync();
            var news = await _newsRepository.GetAllTinTucAsync();

            // Thống kê hóa đơn
            var totalRevenue = invoices.Where(i => i.TrangThai == "Đã thanh toán").Sum(i => i.TongTien);
            var totalProfit = invoices.Where(i => i.TrangThai == "Đã thanh toán").Select(i => i.ThanhToans.First().SoTien).Sum(); // Lợi nhuận là 15% doanh thu
            var invoiceCount = invoices.Count();
            var paidInvoiceCount = invoices.Count(i => i.TrangThai == "Đã thanh toán");
            var unpaidInvoiceCount = invoiceCount - paidInvoiceCount;

            // Thống kê đặt phòng
            var bookingCount = bookings.Count();
            var confirmedBookingCount = bookings.Count(b => b.TrangThai == "Đã xác nhận");
            var pendingBookingCount = bookings.Count(b => b.TrangThai == "Chờ xác nhận");

            // Thống kê người dùng
            var userCount = users.Count();
            var activeUserCount = users.Count(u => u.TrangThai == "Hoạt động");
            var inactiveUserCount = userCount - activeUserCount;

            // Thống kê homestay
            var homestayCount = homestays.Count();
            var activeHomestayCount = homestays.Count(h => h.TrangThai == "Hoạt động");

            // Thống kê tin tức
            var newsCount = await _newsRepository.CountAllTinTucAsync();

            // Thống kê dịch vụ
            var serviceCount = await _serviceRepository.CountAllAsync();

            // Doanh thu và lợi nhuận theo tháng
            var endDate = DateTime.Now;
            var startDate = endDate.AddMonths(-months + 1).AddDays(-endDate.Day + 1).Date;
            var allMonths = new List<MonthlyRevenue>();
            var allProfitMonths = new List<MonthlyProfit>(); // Danh sách lợi nhuận theo tháng
            var allBookingMonths = new List<MonthlyBooking>();
            for (var date = startDate; date <= endDate; date = date.AddMonths(1))
            {
                allMonths.Add(new MonthlyRevenue { Month = $"{date.Month}/{date.Year}", Revenue = 0 });
                allProfitMonths.Add(new MonthlyProfit { Month = $"{date.Month}/{date.Year}", Profit = 0 }); // Khởi tạo lợi nhuận
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

            // Tính lợi nhuận theo tháng (15% của doanh thu)
            var monthlyProfitData = invoices
                .Where(i => i.TrangThai == "Đã thanh toán" && i.NgayLap >= startDate && i.NgayLap <= endDate)
                .GroupBy(i => new { i.NgayLap.Year, i.NgayLap.Month })
                .Select(g => new { MonthKey = $"{g.Key.Month}/{g.Key.Year}", Profit = g.Sum(i => i.TongTien * 0.15m) })
                .ToList();

            foreach (var profit in monthlyProfitData)
            {
                var monthEntry = allProfitMonths.FirstOrDefault(m => m.Month == profit.MonthKey);
                if (monthEntry != null) monthEntry.Profit = profit.Profit;
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

            // Khu vực phổ biến
            var popularAreas = await _khuVucRepository.GetPopularAreasAsync();

            // Tin tức phổ biến
            var popularNews = await _newsRepository.GetPopularNewsAsync();

            // Homestay phổ biến
            var popularHomestays = await _homestayRepository.GetPopularHomestaysAsync();

            // Dịch vụ phổ biến
            var popularServices = await _serviceRepository.GetPopularServicesAsync();

            return new DashboardStatsData
            {
                TotalRevenue = totalRevenue,
                TotalProfit = totalProfit, // Thêm tổng lợi nhuận
                InvoiceCount = invoiceCount,
                PaidInvoiceCount = paidInvoiceCount,
                UnpaidInvoiceCount = unpaidInvoiceCount,
                BookingCount = bookingCount,
                ConfirmedBookingCount = confirmedBookingCount,
                PendingBookingCount = pendingBookingCount,
                UserCount = userCount,
                ActiveUserCount = activeUserCount,
                InactiveUserCount = inactiveUserCount,
                HomestayCount = homestayCount,
                ActiveHomestayCount = activeHomestayCount,
                NewsCount = newsCount,
                ServiceCount = serviceCount,
                MonthlyRevenue = allMonths,
                MonthlyProfit = allProfitMonths, // Thêm lợi nhuận theo tháng
                MonthlyBookings = allBookingMonths,
                PopularHomestays = popularHomestays,
                PopularAreas = popularAreas,
                PopularNews = popularNews,
                PopularServices = popularServices
            };
        }

        private async Task<RecentActivitiesData> GetRecentActivitiesData()
        {
            var recentInvoices = (await _hoaDonRepository.GetAllAsync())
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

            var recentBookings = (await _phieuDatPhongRepository.GetAllAsync())
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

            var newUsers = (await _userRepository.GetAllAsync())
                .OrderByDescending(u => u.NgayTao)
                .Take(5)
                .Select(u => new NewUser
                {
                    Id = u.Id,
                    Name = u.FullName,
                    Email = u.Email,
                    JoinDate = u.NgayTao,
                    Status = u.TrangThai,
                    ProfilePicture = u.ProfilePicture
                })
                .ToList();

            return new RecentActivitiesData
            {
                RecentInvoices = recentInvoices,
                RecentBookings = recentBookings,
                NewUsers = newUsers
            };
        }
    }
}
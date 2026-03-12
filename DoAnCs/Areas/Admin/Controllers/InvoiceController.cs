using DoAnCs.Areas.Admin.ModelsView;
using DoAnCs.Models;
using DoAnCs.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace DoAnCs.Areas.Admin.Controllers
{
    public class InvoiceController : BaseController
    {
        private readonly IHoaDonRepository _hoaDonRepository; 
        private readonly IPhieuDatPhongRepository _phieuDatPhongRepository; 
        private readonly IKhuyenMaiRepository _khuyenMaiRepository; 
        private readonly IThanhToanRepository _thanhToanRepository; 
        private readonly IPhongRepository _phongRepository; 
        private readonly IServiceRepository _serviceRepository; 
        private readonly IHomestayRepository _homestayRepository;

        public InvoiceController(
            IHoaDonRepository hoaDonRepository,
            IPhieuDatPhongRepository phieuDatPhongRepository,
            IKhuyenMaiRepository khuyenMaiRepository,
            IThanhToanRepository thanhToanRepository,
            IPhongRepository phongRepository,
            IServiceRepository serviceRepository,
            IHomestayRepository homestayRepository)
        {
            _hoaDonRepository = hoaDonRepository;
            _phieuDatPhongRepository = phieuDatPhongRepository;
            _khuyenMaiRepository = khuyenMaiRepository;
            _thanhToanRepository = thanhToanRepository;
            _phongRepository = phongRepository;
            _serviceRepository = serviceRepository;
            _homestayRepository = homestayRepository;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetInvoices(string searchString, string statusFilter, string dateRange, int page = 1, int pageSize = 10)
        {
            try
            {
                var query = (await _hoaDonRepository.GetAllAsync()).AsQueryable();

                if (!string.IsNullOrEmpty(searchString))
                {
                    query = query.Where(hd => hd.Ma_HD.Contains(searchString) || hd.NguoiDung.FullName.Contains(searchString));
                }

                if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "all")
                {
                    query = query.Where(hd => hd.TrangThai == statusFilter);
                }

                if (!string.IsNullOrEmpty(dateRange))
                {
                    var dates = dateRange.Split(" - ");
                    if (dates.Length == 2)
                    {
                        var format = "dd/MM/yyyy";
                        var culture = CultureInfo.InvariantCulture;
                        if (DateTime.TryParseExact(dates[0], format, culture, DateTimeStyles.None, out var startDate) &&
                            DateTime.TryParseExact(dates[1], format, culture, DateTimeStyles.None, out var endDate))
                        {
                            endDate = endDate.Date.AddDays(1).AddTicks(-1);
                            query = query.Where(hd => hd.NgayLap >= startDate && hd.NgayLap <= endDate);
                        }
                        else
                        {
                            Console.WriteLine($"Failed to parse dateRange: {dateRange}");
                            return Json(new { success = false, message = "Định dạng ngày không hợp lệ." });
                        }
                    }
                }

                var totalItems = query.Count();
                var invoices = query
                    .OrderByDescending(hd => hd.NgayLap)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(hd => new
                    {
                        Ma_HD = hd.Ma_HD,
                        CustomerName = hd.NguoiDung.FullName,
                        NgayLap = hd.NgayLap,
                        TongTien = hd.TongTien,
                        Thue = hd.Thue,
                        TrangThai = hd.TrangThai,
                        MaPDPhongs = hd.ChiTietHoaDons.Select(ct => ct.Ma_PDPhong).ToList(),
                        KhuyenMais = hd.ApDungKMs.Select(ad => new { ad.Ma_KM, ad.KhuyenMai.NoiDung, ad.KhuyenMai.ChietKhau }).ToList(),
                        ThanhToans = hd.ThanhToans.Select(tt => new { tt.MaTT, tt.SoTien, tt.PhuongThuc, tt.NgayTT, tt.TrangThai }).ToList()
                    })
                    .ToList();

                return Json(new
                {
                    success = true,
                    data = invoices,
                    totalItems,
                    totalPages = (int)Math.Ceiling((double)totalItems / pageSize),
                    currentPage = page
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi khi lấy danh sách hóa đơn: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            try
            {
                var hoaDon = await _hoaDonRepository.GetByIdAsync(id);
                if (hoaDon == null)
                    return Json(new { success = false, message = "Không tìm thấy hóa đơn" });

                var result = new
                {
                    Ma_HD = hoaDon.Ma_HD,
                    CustomerName = hoaDon.NguoiDung.FullName,
                    NgayLap = hoaDon.NgayLap.ToString("dd/MM/yyyy"),
                    TongTien = hoaDon.TongTien,
                    Thue = hoaDon.Thue,
                    TrangThai = hoaDon.TrangThai,
                    MaPDPhongs = hoaDon.ChiTietHoaDons.Select(ct => ct.Ma_PDPhong).ToList(),
                    KhuyenMais = hoaDon.ApDungKMs.Select(ad => new { ad.Ma_KM, ad.KhuyenMai.NoiDung, ad.KhuyenMai.ChietKhau }).ToList(),
                    ThanhToans = hoaDon.ThanhToans.Select(tt => new { tt.MaTT, tt.SoTien, tt.PhuongThuc, tt.NgayTT, tt.TrangThai, tt.NoiDung }).ToList()
                };

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi khi lấy chi tiết hóa đơn: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailableBookings()
        {
            try
            {
                var bookings = await _phieuDatPhongRepository.GetAllAsync();
                if (bookings == null || !bookings.Any())
                {
                    return Json(new { success = false, message = "Không tìm thấy phiếu đặt phòng" });
                }

                var availableBookings = bookings
                    .Where(pdp => pdp.TrangThai == "Chờ xác nhận" &&
                                  (pdp.ChiTietHoaDons == null || !pdp.ChiTietHoaDons.Any() || pdp.ChiTietHoaDons.Any(ct => ct.HoaDon?.TrangThai != "Đã thanh toán")))
                    .Select(pdp => new
                    {
                        Ma_PDPhong = pdp.Ma_PDPhong,
                        CustomerName = pdp.NguoiDung?.FullName ?? "Khách hàng không xác định",
                        Rooms = pdp.ChiTietDatPhongs.Select(ct => new
                        {
                            TenPhong = ct.Phong.TenPhong,
                            NgayDen = ct.NgayDen.ToString("dd/MM/yyyy"),
                            NgayDi = ct.NgayDi.ToString("dd/MM/yyyy"),
                            DonGia = ct.Phong?.DonGia ?? 0
                        }).ToList()
                    })
                    .ToList();

                return Json(new { success = true, data = availableBookings });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi khi lấy danh sách phiếu đặt phòng: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetBookingDetails(string maPDPhong)
        {
            try
            {
                if (string.IsNullOrEmpty(maPDPhong))
                {
                  
                    return Json(new { success = false, message = "Mã phiếu đặt phòng không hợp lệ" });
                }

                var booking = await _phieuDatPhongRepository.GetByIdAsync(maPDPhong);
                if (booking == null)
                {
                   
                    return Json(new { success = false, message = "Không tìm thấy phiếu đặt phòng" });
                }

                var chiTietDatPhongs = booking.ChiTietDatPhongs ?? new List<ChiTietDatPhong>();
                decimal totalPrice = 0;
                var rooms = new List<object>();
                foreach (var ct in chiTietDatPhongs)
                {
                    var phong = ct.Phong; // Sử dụng dữ liệu đã Include
                    var homestay = phong?.Homestay; // Sử dụng dữ liệu đã Include
                    var days = (ct.NgayDi != DateTime.MinValue && ct.NgayDen != DateTime.MinValue) ? (ct.NgayDi - ct.NgayDen).Days : 0;
                    var roomPrice = days * (phong?.DonGia ?? 0);
                    totalPrice += roomPrice;

                    rooms.Add(new
                    {
                        tenPhong = phong?.TenPhong ?? "Không xác định",
                        donGia = phong?.DonGia ?? 0,
                        soNguoi = phong?.SoNguoi ?? 0,
                        ngayDen = ct.NgayDen != DateTime.MinValue ? ct.NgayDen.ToString("dd/MM/yyyy") : "Không xác định",
                        ngayDi = ct.NgayDi != DateTime.MinValue ? ct.NgayDi.ToString("dd/MM/yyyy") : "Không xác định",
                        soNgay = days,
                        thanhTien = roomPrice,
                        homestay = homestay != null ? new { tenHomestay = homestay.Ten_Homestay, diaChi = homestay.DiaChi } : null
                    });
                }

                var services = new List<object>();
                foreach (var ct in chiTietDatPhongs)
                {
                    var phieuSuDungDVs = ct.PhieuSuDungDVs ?? new List<PhieuSuDungDV>();
                    foreach (var ps in phieuSuDungDVs)
                    {
                        var chiTietPhieuDVs = ps.ChiTietPhieuDVs ?? new List<ChiTietPhieuDV>();
                        foreach (var ctdv in chiTietPhieuDVs)
                        {
                            var dichVu = ctdv.DichVu; // Sử dụng dữ liệu đã Include
                            var servicePrice = ctdv.SoLuong * (dichVu?.DonGia ?? 0);
                            totalPrice += servicePrice;

                            services.Add(new
                            {
                                tenDV = dichVu?.Ten_DV ?? "Không xác định",
                                donGia = dichVu?.DonGia ?? 0,
                                soLuong = ctdv.SoLuong,
                                ngaySuDung = ctdv.NgaySuDung != DateTime.MinValue ? ctdv.NgaySuDung.ToString("dd/MM/yyyy") : "Không xác định",
                                thanhTien = servicePrice
                            });
                        }
                    }
                }

                var result = new
                {
                    maPDPhong = booking.Ma_PDPhong,
                    customerName = booking.NguoiDung?.FullName ?? "Khách hàng không xác định",
                    ngayLap = booking.NgayLap != DateTime.MinValue ? booking.NgayLap.ToString("dd/MM/yyyy") : "Không xác định",
                    trangThai = booking.TrangThai ?? "Không xác định",
                    rooms,
                    services,
                    totalPrice
                };

               
                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
              
                return Json(new { success = false, message = $"Lỗi khi lấy chi tiết phiếu đặt phòng: {ex.Message}" });
            }
        }
    }
}
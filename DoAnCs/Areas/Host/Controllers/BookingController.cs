using DoAnCs.Models;
using DoAnCs.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;
using DoAnCs.Areas.Admin.ModelsView;

namespace DoAnCs.Areas.Host.Controllers
{
    [Area("Host")]
    [Route("Host/Booking")]
    [Authorize(Roles = "Host")]
    public class BookingController : Controller
    {
        private readonly IPhieuDatPhongRepository _phieuDatPhongRepo;
        private readonly IUserRepository _userRepo;
        private readonly IPhongRepository _phongRepo;
        private readonly IServiceRepository _dichVuRepo;
        private readonly IHomestayRepository _homestayRepo;
        private readonly ApplicationDbContext _context;
        private readonly IHoaDonRepository _hoaDonRepo;
        private readonly IKhuyenMaiRepository _khuyenMaiRepo;
        private readonly IPhuThuRepository _phuThuRepo;
        private readonly IHuyPhongRepository _huyPhongRepo;

        public BookingController(
            IPhieuDatPhongRepository phieuDatPhongRepo,
            IUserRepository userRepo,
            IPhongRepository phongRepo,
            IServiceRepository dichVuRepo,
            IHomestayRepository homestayRepo,
            ApplicationDbContext context,
            IHoaDonRepository hoaDonRepo,
            IKhuyenMaiRepository khuyenMaiRepo,
            IPhuThuRepository phuThuRepo,
            IHuyPhongRepository huyPhongRepo)
        {
            _phieuDatPhongRepo = phieuDatPhongRepo;
            _userRepo = userRepo;
            _phongRepo = phongRepo;
            _dichVuRepo = dichVuRepo;
            _homestayRepo = homestayRepo;
            _context = context;
            _hoaDonRepo = hoaDonRepo;
            _khuyenMaiRepo = khuyenMaiRepo;
            _phuThuRepo = phuThuRepo;
            _huyPhongRepo = huyPhongRepo;
        }

        [HttpGet("Index")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("GetBookings")]
        public async Task<JsonResult> GetBookings(
            string searchQuery = "",
            string statusFilter = "all",
            string dateRange = "",
            int page = 1,
            int pageSize = 10,
            string sortBy = "newest")
        {
            try
            {
                // Lấy ID của host từ User
                var hostId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(hostId))
                {
                    return Json(new { success = false, message = "Không xác định được host" });
                }

                // Lấy danh sách phiếu đặt phòng liên quan đến homestay của host
                var bookings = await _phieuDatPhongRepo.GetByHostIdAsync(hostId);

                if (!string.IsNullOrEmpty(searchQuery))
                {
                    searchQuery = searchQuery.ToLower();
                    bookings = bookings.Where(b =>
                        b.NguoiDung.FullName.ToLower().Contains(searchQuery) ||
                        b.Ma_PDPhong.ToLower().Contains(searchQuery)).ToList();
                }

                if (statusFilter != "all")
                {
                    bookings = bookings.Where(b => b.TrangThai == statusFilter).ToList();
                }

                if (!string.IsNullOrWhiteSpace(dateRange))
                {
                    var dates = dateRange.Trim().Split(" - ");
                    if (dates.Length == 2)
                    {
                        var formats = new[] { "dd/MM/yyyy", "d/M/yyyy" };
                        if (DateTime.TryParseExact(dates[0].Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var startDate) &&
                            DateTime.TryParseExact(dates[1].Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var endDate))
                        {
                            if (startDate <= endDate)
                            {
                                bookings = bookings.Where(b =>
                                    b.NgayLap.Date >= startDate.Date &&
                                    b.NgayLap.Date <= endDate.Date).ToList();
                            }
                            else
                            {
                                return Json(new { success = false, message = "Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc" });
                            }
                        }
                        else
                        {
                            return Json(new { success = false, message = "Định dạng khoảng ngày không hợp lệ. Vui lòng chọn cả ngày bắt đầu và ngày kết thúc (dd/MM/yyyy - dd/MM/yyyy)." });
                        }
                    }
                    else
                    {
                        return Json(new { success = false, message = "Khoảng ngày không hợp lệ. Vui lòng chọn cả ngày bắt đầu và ngày kết thúc (dd/MM/yyyy - dd/MM/yyyy)." });
                    }
                }

                bookings = sortBy == "newest"
                    ? bookings.OrderByDescending(b => b.NgayLap).ToList()
                    : bookings.OrderBy(b => b.NgayLap).ToList();

                var totalCount = bookings.Count();

                bookings = bookings
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var bookingList = bookings.Select(b => new
                {
                    b.Ma_PDPhong,
                    CustomerName = b.NguoiDung.FullName,
                    CustomerEmail = b.NguoiDung.Email,
                    CustomerPhone = b.NguoiDung.PhoneNumber,
                    Rooms = b.ChiTietDatPhongs.Select(ct => new
                    {
                        RoomName = ct.Phong.TenPhong,
                        HomestayName = ct.Phong.Homestay.Ten_Homestay,
                        RoomPrice = ct.Phong.DonGia,
                        DateRange = $"{ct.NgayDen:dd/MM/yyyy} - {ct.NgayDi:dd/MM/yyyy}",
                        Services = ct.PhieuSuDungDVs.SelectMany(p => p.ChiTietPhieuDVs.Select(s => new
                        {
                            Name = s.DichVu.Ten_DV,
                            Quantity = s.SoLuong,
                            Price = s.DichVu.DonGia
                        }))
                    }),
                    b.NgayLap,
                    b.TrangThai,
                    TotalPrice = CalculateTotalPrice(b)
                }).ToList();

                return Json(new
                {
                    success = true,
                    data = bookingList,
                    totalCount
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Không thể tải danh sách đặt phòng: " + ex.Message });
            }
        }

        private decimal CalculateTotalPrice(PhieuDatPhong booking)
        {
            decimal total = 0;

            foreach (var detail in booking.ChiTietDatPhongs)
            {
                var nights = (detail.NgayDi - detail.NgayDen).Days;
                total += nights * (detail.Phong.DonGia + _phuThuRepo.CalculatePhuThuAsync(detail.Phong.ID_Loai, detail.NgayDen, detail.NgayDi, detail.Phong.DonGia).Result);
                var hoaDon = _hoaDonRepo.GetByPhieuDatPhongAsync(booking.Ma_PDPhong).Result;
                if (hoaDon != null)
                {
                    if (hoaDon.ApDungKMs != null && hoaDon.ApDungKMs.Any())
                    {
                        var khuyenMai = _khuyenMaiRepo.GetByIdAsync(hoaDon.ApDungKMs.First().Ma_KM).Result;
                        if (khuyenMai != null)
                        {
                            total -= total * (khuyenMai.ChietKhau / 100);
                        }
                    }
                }
                foreach (var _service in detail.PhieuSuDungDVs)
                {
                    total += _service.ChiTietPhieuDVs.Sum(ct => ct.SoLuong * ct.DichVu.DonGia);
                }
            }
            return total;
        }
        private decimal CalculateTotalPriceHaveToPay(PhieuDatPhong booking)
        {
            decimal total = 0;
            decimal dv = 0;
            foreach (var detail in booking.ChiTietDatPhongs)
            {
                var nights = (detail.NgayDi - detail.NgayDen).Days;
                total += nights * (detail.Phong.DonGia + _phuThuRepo.CalculatePhuThuAsync(detail.Phong.ID_Loai, detail.NgayDen, detail.NgayDi, detail.Phong.DonGia).Result);
                var hoaDon = _hoaDonRepo.GetByPhieuDatPhongAsync(booking.Ma_PDPhong).Result;
                if (hoaDon != null)
                {
                    if (hoaDon.ApDungKMs != null && hoaDon.ApDungKMs.Any())
                    {
                        var khuyenMai = _khuyenMaiRepo.GetByIdAsync(hoaDon.ApDungKMs.First().Ma_KM).Result;
                        if (khuyenMai != null)
                        {
                            total -= total * (khuyenMai.ChietKhau / 100);
                        }
                    }
                }
                foreach (var _service in detail.PhieuSuDungDVs)
                {
                    dv += _service.ChiTietPhieuDVs.Sum(ct => ct.SoLuong * ct.DichVu.DonGia);
                }
            }
            total *= 0.85m; // tiền khấu trừ 15% phí từ hoa hồng cho website
            total += dv; // cộng tiền dịch vụ
            return total;
        }
        [HttpGet("Details/{id}")]
        public async Task<JsonResult> Details(string id)
        {
            try
            {
                var hostId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(hostId))
                {
                    return Json(new { success = false, message = "Không xác định được host" });
                }

                var booking = await _phieuDatPhongRepo.GetByIdAsync(id);
                if (booking == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy phiếu đặt phòng" });
                }

                // Kiểm tra xem phiếu đặt phòng có thuộc homestay của host không
                var isHostBooking = booking.ChiTietDatPhongs.Any(ct => ct.Phong.Homestay.Ma_ND == hostId);
                if (!isHostBooking)
                {
                    return Json(new { success = false, message = "Bạn không có quyền xem phiếu đặt phòng này" });
                }

                var roomTasks = booking.ChiTietDatPhongs.Select(async ct => new
                {
                    RoomPrice = ct.Phong?.DonGia,
                    RoomId = ct.Ma_Phong,
                    RoomName = ct.Phong?.TenPhong,
                    HomestayId = ct.Phong?.ID_Homestay,
                    HomestayName = ct.Phong?.Homestay?.Ten_Homestay,
                    CheckinDate = ct.NgayDen.ToString("yyyy-MM-dd"),
                    CheckoutDate = ct.NgayDi.ToString("yyyy-MM-dd"),
                    Services = ct.PhieuSuDungDVs?.SelectMany(p =>
                        p.ChiTietPhieuDVs?.Select(s => new
                        {
                            Id = s.Ma_DV,
                            Name = s.DichVu?.Ten_DV,
                            Quantity = s.SoLuong,
                            Price = s.DichVu?.DonGia
                        }) ?? Enumerable.Empty<object>()
                    )?.ToList() ?? new List<object>(),
                    Surcharges = ct.Phong != null && ct.Phong.ID_Loai != null
                        ? (await _phuThuRepo.GetApDungPhuThuByLoaiPhongAsync(
                            ct.Phong.ID_Loai,
                            ct.NgayDen,
                            ct.NgayDi)).Select(ap => (object)new
                            {
                                SurchargeId = ap.Ma_PhieuPT,
                                Description = ap.PhieuPhuThu?.NoiDung ?? "Không xác định",
                                Amount = ap.PhieuPhuThu != null && ct.Phong != null
                                    ? ap.PhieuPhuThu.PhiPhuThu * ct.Phong.DonGia
                                    : 0,
                                AppliedDate = ap.NgayApDung.ToString("yyyy-MM-dd")
                            }).ToList()
                        : new List<object>()
                }).ToArray();

                var rooms = await Task.WhenAll(roomTasks);

                var result = new
                {
                    booking.Ma_PDPhong,
                    CustomerName = booking.NguoiDung?.FullName,
                    CustomerImage = booking.NguoiDung?.ProfilePicture,
                    CustomerEmail = booking.NguoiDung?.Email,
                    CustomerPhone = booking.NguoiDung?.PhoneNumber,
                    Rooms = rooms,
                    booking.NgayLap,
                    booking.TrangThai,
                    TotalPrice = CalculateTotalPrice(booking),
                    HaveToPay = booking.TrangThai == "Đã xác nhận" ? CalculateTotalPriceHaveToPay(booking) : (decimal?)null
                };

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Không thể tải chi tiết đặt phòng: " + ex.Message });
            }
        }

        [HttpPost("UpdateStatus")]
        public async Task<JsonResult> UpdateStatus([FromBody] UpdateStatusModel model)
        {
            try
            {
                var hostId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(hostId))
                {
                    return Json(new { success = false, message = "Không xác định được host" });
                }

                var booking = await _phieuDatPhongRepo.GetByIdAsync(model.MaPDPhong);
                if (booking == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy phiếu đặt phòng" });
                }

                // Kiểm tra xem phiếu đặt phòng có thuộc homestay của host không
                var isHostBooking = booking.ChiTietDatPhongs.Any(ct => ct.Phong.Homestay.Ma_ND == hostId);
                if (!isHostBooking)
                {
                    return Json(new { success = false, message = "Bạn không có quyền cập nhật phiếu đặt phòng này" });
                }

                using var transaction = await _context.Database.BeginTransactionAsync();

                booking.TrangThai = model.TrangThai;
                await _phieuDatPhongRepo.UpdateAsync(booking);
                await transaction.CommitAsync();

                return Json(new { success = true, message = "Cập nhật trạng thái thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Không thể cập nhật trạng thái: " + ex.Message });
            }
        }

        [HttpPost("CancelBooking")]
        public async Task<JsonResult> CancelBooking([FromBody] CancelBookingModel model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var hostId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(hostId))
                {
                    return Json(new { success = false, message = "Không xác định được host" });
                }

                var phieuDatPhong = await _phieuDatPhongRepo.GetByIdAsync(model.Ma_PDPhong);
                if (phieuDatPhong == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy phiếu đặt phòng" });
                }

                // Kiểm tra xem phiếu đặt phòng có thuộc homestay của host không
                var isHostBooking = phieuDatPhong.ChiTietDatPhongs.Any(ct => ct.Phong.Homestay.Ma_ND == hostId);
                if (!isHostBooking)
                {
                    return Json(new { success = false, message = "Bạn không có quyền hủy phiếu đặt phòng này" });
                }

                if (phieuDatPhong.TrangThai == "Đã hủy")
                {
                    return Json(new { success = false, message = "Phiếu đã bị hủy trước đó" });
                }

                var chiTietDatPhongs = phieuDatPhong.ChiTietDatPhongs
                    .Where(ct => ct.Phong.Homestay.Ma_ND == hostId)
                    .ToList();
                if (!chiTietDatPhongs.Any())
                {
                    return Json(new { success = false, message = "Không tìm thấy chi tiết đặt phòng" });
                }

                var earliestCheckinDate = chiTietDatPhongs.Min(ct => ct.NgayDen);
                var cancelDeadline = earliestCheckinDate.AddDays(-1);
                if (DateTime.Now >= cancelDeadline)
                {
                    return Json(new { success = false, message = "Không thể hủy vì đã quá thời hạn (trước ngày nhận phòng 1 ngày)" });
                }

                var hoaDon = await _hoaDonRepo.GetByPhieuDatPhongAsync(model.Ma_PDPhong);
                if (hoaDon != null)
                {
                    if (hoaDon.ApDungKMs != null && hoaDon.ApDungKMs.Any())
                    {
                        _context.ApDungKMs.RemoveRange(hoaDon.ApDungKMs);
                    }

                    await _hoaDonRepo.DeleteAsync(hoaDon.Ma_HD);
                }

                var phieuHuy = new PhieuHuyPhong
                {
                    MaPHP = "PHP-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                    Ma_PDPhong = model.Ma_PDPhong,
                    LyDo = model.LyDo,
                    NgayHuy = DateTime.Now,
                    NguoiHuy = "Host",
                    TrangThai = "Đã hủy"
                };
                await _huyPhongRepo.AddAsync(phieuHuy);

                phieuDatPhong.TrangThai = "Đã hủy";
                await _phieuDatPhongRepo.UpdateAsync(phieuDatPhong);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true, message = "Hủy phòng thành công" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = "Không thể hủy phòng: " + ex.Message });
            }
        }
    }
}
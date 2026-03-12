using DoAnCs.Models;
using DoAnCs.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using DoAnCs.Areas.Admin.ModelsView;
using ClosedXML.Excel;

namespace DoAnCs.Areas.Admin.Controllers
{
   
    [Route("Admin/Booking")]
    public class BookingController : BaseController
    {
        private readonly IPhieuDatPhongRepository _phieuDatPhongRepo;
        private readonly IHuyPhongRepository _huyPhongRepo;
        private readonly IUserRepository _userRepo;
        private readonly IPhongRepository _phongRepo;
        private readonly IServiceRepository _dichVuRepo;
        private readonly IHomestayRepository _homestayRepo;
        private readonly ApplicationDbContext _context;
        private readonly IHoaDonRepository _hoaDonRepo;
        private readonly IKhuyenMaiRepository _khuyenMaiRepo;
        private readonly IPhuThuRepository _phuThuRepo;
        public BookingController(
            IPhieuDatPhongRepository phieuDatPhongRepo,
            IUserRepository userRepo,
            IPhongRepository phongRepo,
            IServiceRepository dichVuRepo,
            IHomestayRepository homestayRepo,
            ApplicationDbContext context,
            IHoaDonRepository hoaDonRepo,
            IKhuyenMaiRepository khuyenMaiRepo,
            IPhuThuRepository phuThuRepo, IHuyPhongRepository huyPhongRepo)
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
                var bookings = await _phieuDatPhongRepo.GetAllAsync();

                if (!string.IsNullOrEmpty(searchQuery))
                {
                    searchQuery = searchQuery.ToLower();
                    bookings = bookings.Where(b =>
                        b.NguoiDung.FullName.ToLower().Contains(searchQuery) ||
                        b.Ma_PDPhong.ToLower().Contains(searchQuery));
                }

                if (statusFilter != "all")
                {
                    bookings = bookings.Where(b => b.TrangThai == statusFilter);
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
                                    b.NgayLap.Date <= endDate.Date);
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
                    ? bookings.OrderByDescending(b => b.NgayLap)
                    : bookings.OrderBy(b => b.NgayLap);

                var totalCount = bookings.Count();

                bookings = bookings
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize);

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
                foreach (var service in detail.PhieuSuDungDVs)
                {
                    total += service.ChiTietPhieuDVs.Sum(ct => ct.SoLuong * ct.DichVu.DonGia);
                }
              
            }
            return total;
        }

        [HttpGet("Details/{id}")]
        public async Task<JsonResult> Details(string id)
        {
            try
            {
                var booking = await _phieuDatPhongRepo.GetByIdAsync(id);
                if (booking == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy phiếu đặt phòng" });
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
                    TotalPrice = CalculateTotalPrice(booking)
                };

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Không thể tải chi tiết đặt phòng: " + ex.Message });
            }
        }

        [HttpGet("GetUsers")]
        public async Task<JsonResult> GetUsers(string search = "")
        {
            try
            {
                var users = await _userRepo.GetAllAsync();

                if (!string.IsNullOrEmpty(search))
                {
                    search = search.ToLower();
                    users = users.Where(u =>
                        u.FullName.ToLower().Contains(search) ||
                        u.Email.ToLower().Contains(search) ||
                        (u.PhoneNumber != null && u.PhoneNumber.Contains(search)));
                }

                var userList = users.Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.Email,
                    u.PhoneNumber,
                    u.ProfilePicture
                }).ToList();

                return Json(new { success = true, data = userList });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Không thể tải danh sách người dùng: " + ex.Message });
            }
        }

        [HttpGet("GetPhongsByHomestay")]
        public async Task<JsonResult> GetPhongsByHomestay(string homestayId)
        {
            try
            {
                if (string.IsNullOrEmpty(homestayId))
                {
                    return Json(new { success = false, message = "HomestayId là bắt buộc" });
                }

                var phongs = await _phongRepo.GetByHomestayAsync(homestayId);

                var phongList = phongs
                    .Where(p => p.TrangThai != "Bảo trì")
                    .Select(p => new
                    {
                        p.Ma_Phong,
                        p.TenPhong,
                        p.DonGia,
                        p.LoaiPhong.TenLoai,
                        ImageUrl = p.HinhAnhPhongs.FirstOrDefault(s => s.LaAnhChinh)?.UrlAnh,
                        HomestayId = p.ID_Homestay,
                        HomestayName = p.Homestay?.Ten_Homestay ?? "Không xác định"
                    }).ToList();

                return Json(new { success = true, data = phongList });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Không thể tải danh sách phòng: " + ex.Message });
            }
        }

        [HttpGet("GetUnavailableDates")]
        public async Task<JsonResult> GetUnavailableDates(string roomId)
        {
            try
            {
                if (string.IsNullOrEmpty(roomId))
                {
                    return Json(new { success = false, message = "RoomId là bắt buộc" });
                }

                var bookedDates = await _context.ChiTietDatPhongs
                    .Where(ct => ct.Ma_Phong == roomId &&
                                 ct.PhieuDatPhong.TrangThai == "Đã xác nhận")
                    .Select(ct => new
                    {
                        Start = ct.NgayDen,
                        End = ct.NgayDi
                    })
                    .ToListAsync();

                var unavailableDates = new List<string>();
                foreach (var booking in bookedDates)
                {
                    var currentDate = booking.Start.Date;
                    var endDate = booking.End.Date;
                    while (currentDate <= endDate)
                    {
                        unavailableDates.Add(currentDate.ToString("yyyy-MM-dd"));
                        currentDate = currentDate.AddDays(1);
                    }
                }

                return Json(new { success = true, data = unavailableDates.Distinct().ToList() });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Không thể tải danh sách ngày không khả dụng: " + ex.Message });
            }
        }

        [HttpGet("GetDichVusByHomestay")]
        public async Task<JsonResult> GetDichVusByHomestay(string homestayId)
        {
            try
            {
                var dichVus = await _dichVuRepo.GetByHomestayAsync(homestayId);

                var dichVuList = dichVus.Select(d => new
                {
                    d.Ma_DV,
                    d.Ten_DV,
                    d.DonGia,
                    d.HinhAnh
                }).ToList();

                return Json(new { success = true, data = dichVuList });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Không thể tải danh sách dịch vụ: " + ex.Message });
            }
        }

        [HttpGet("GetAllHomestays")]
        public async Task<JsonResult> GetAllHomestays()
        {
            try
            {
                var homestays = await _homestayRepo.GetAllAsync();

                var homestayList = homestays
                    .Where(s => s.TrangThai == "Hoạt động")
                    .Select(h => new
                    {
                        id_Homestay = h.ID_Homestay,
                        tenHomestay = h.Ten_Homestay,
                        diaChi = h.DiaChi
                    }).ToList();

                return Json(new { success = true, data = homestayList });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Không thể tải danh sách homestay: " + ex.Message });
            }
        }

        [HttpPost("UpdateStatus")]
        public async Task<JsonResult> UpdateStatus([FromBody] UpdateStatusModel model)
        {
            try
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                var booking = await _phieuDatPhongRepo.GetByIdAsync(model.MaPDPhong);
                if (booking == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy phiếu đặt phòng" });
                }

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

        [HttpPost("Delete/{id}")]
        public async Task<JsonResult> Delete(string id)
        {
            try
            {
                var booking = await _phieuDatPhongRepo.GetByIdAsync(id);
                if (booking == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy phiếu đặt phòng" });
                }
                if (booking.TrangThai == "Đã xác nhận")
                {
                    return Json(new { success = false, message = "Không thể xóa phiếu đặt phòng đã đặt phải hủy trước" });
                }
                await _phieuDatPhongRepo.DeleteAsync(id);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Xóa phiếu đặt phòng thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Không thể xóa phiếu đặt phòng: " + ex.Message });
            }
        }
        [HttpGet("Cancellation")]
        public async Task<ActionResult> Cancellation()
        {
            return View();
        }
        [HttpGet("GetCancellations")]
        public async Task<JsonResult> GetCancellations(
            string searchQuery = "",
            string statusFilter = "all",
            string dateRange = "",
            string homestayId = "",
            int page = 1,
            int pageSize = 10,
            string sortBy = "newest")
        {
            try
            {
                var cancellations = await _context.PhieuHuyPhongs
                    .Include(hp => hp.PhieuDatPhong)
                    .ThenInclude(pd => pd.NguoiDung)
                    .Include(hp => hp.PhieuDatPhong)
                    .ThenInclude(pd => pd.ChiTietDatPhongs)
                    .ThenInclude(ct => ct.Phong)
                    .ThenInclude(p => p.Homestay)
                    .ToListAsync();
               

                if (!string.IsNullOrEmpty(searchQuery))
                {
                    searchQuery = searchQuery.ToLower();
                    cancellations = cancellations.Where(hp => hp.MaPHP.ToLower().Contains(searchQuery)).ToList();
                }

                if (statusFilter != "all")
                {
                    cancellations = cancellations.Where(hp => hp.TrangThai == statusFilter).ToList();
                }

                if (!string.IsNullOrEmpty(homestayId))
                {
                    cancellations = cancellations.Where(hp => hp.PhieuDatPhong.ChiTietDatPhongs
                        .Any(ct => ct.Phong.ID_Homestay == homestayId)).ToList();
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
                                cancellations = cancellations.Where(hp =>
                                    hp.NgayHuy.Date >= startDate.Date &&
                                    hp.NgayHuy.Date <= endDate.Date).ToList();
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
                else
                {
                    cancellations = cancellations.Where(hp => hp.NgayHuy.Date <= DateTime.Today).ToList();
                }

                cancellations = sortBy == "newest"
                    ? cancellations.OrderByDescending(hp => hp.NgayHuy).ToList()
                    : cancellations.OrderBy(hp => hp.NgayHuy).ToList();

                var totalCount = cancellations.Count();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                cancellations = cancellations
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var cancellationList = cancellations.Select(hp => new
                {
                    hp.MaPHP,
                    BookingId = hp.Ma_PDPhong,
                    CustomerName = hp.PhieuDatPhong?.NguoiDung?.FullName ?? "Không xác định",
                    HomestayName = hp.PhieuDatPhong?.ChiTietDatPhongs?.FirstOrDefault()?.Phong?.Homestay?.Ten_Homestay ?? "Không xác định",
                    CancellationDate = hp.NgayHuy.ToString("dd/MM/yyyy"),
                    hp.LyDo,
                    hp.NguoiHuy,
                    hp.TrangThai,
                    hp.TenNganHang,
                    hp.SoTaiKhoan
                }).ToList();

                return Json(new
                {
                    success = true,
                    data = cancellationList,
                    totalCount,
                    totalPages,
                    currentPage = page
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Không thể tải danh sách phiếu hủy phòng: " + ex.Message });
            }
        }
        [HttpGet("ExportCancellationsToExcel")]
        public async Task<IActionResult> ExportCancellationsToExcel(
    string searchQuery = "",
    string homestayId = "",
    string dateRange = "",
    string sortBy = "newest",
    string exportOption = "all")
        {
            try
            {
                var cancellations = await _context.PhieuHuyPhongs
                    .Include(hp => hp.PhieuDatPhong)
                    .ThenInclude(pd => pd.NguoiDung)
                    .Include(hp => hp.PhieuDatPhong)
                    .ThenInclude(pd => pd.ChiTietDatPhongs)
                    .ThenInclude(ct => ct.Phong)
                    .ThenInclude(p => p.Homestay)
                    .ToListAsync();

                // Áp dụng các bộ lọc tương tự GetCancellations
                if (!string.IsNullOrEmpty(searchQuery))
                {
                    searchQuery = searchQuery.ToLower();
                    cancellations = cancellations.Where(hp => hp.MaPHP.ToLower().Contains(searchQuery)).ToList();
                }

                if (!string.IsNullOrEmpty(homestayId))
                {
                    cancellations = cancellations.Where(hp => hp.PhieuDatPhong.ChiTietDatPhongs
                        .Any(ct => ct.Phong.ID_Homestay == homestayId)).ToList();
                }

                if (exportOption == "pending")
                {
                    cancellations = cancellations.Where(hp => hp.TrangThai == "Chờ xử lý").ToList();
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
                                cancellations = cancellations.Where(hp =>
                                    hp.NgayHuy.Date >= startDate.Date &&
                                    hp.NgayHuy.Date <= endDate.Date).ToList();
                            }
                            else
                            {
                                return BadRequest(new { success = false, message = "Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc" });
                            }
                        }
                        else
                        {
                            return BadRequest(new { success = false, message = "Định dạng khoảng ngày không hợp lệ" });
                        }
                    }
                    else
                    {
                        return BadRequest(new { success = false, message = "Khoảng ngày không hợp lệ" });
                    }
                }
                else
                {
                    cancellations = cancellations.Where(hp => hp.NgayHuy.Date <= DateTime.Today).ToList();
                }

                cancellations = sortBy == "newest"
                    ? cancellations.OrderByDescending(hp => hp.NgayHuy).ToList()
                    : cancellations.OrderBy(hp => hp.NgayHuy).ToList();

                // Tạo file Excel sử dụng ClosedXML
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("PhieuHuyPhong");

                // Định nghĩa tiêu đề
                worksheet.Cell(1, 1).Value = "Mã Phiếu Hủy";
                worksheet.Cell(1, 2).Value = "Mã Đặt Phòng";
                worksheet.Cell(1, 3).Value = "Tên Khách Hàng";
                worksheet.Cell(1, 4).Value = "Homestay";
                worksheet.Cell(1, 5).Value = "Ngày Hủy";
                worksheet.Cell(1, 6).Value = "Lý Do";
                worksheet.Cell(1, 7).Value = "Người Hủy";
                worksheet.Cell(1, 8).Value = "Trạng Thái";
                worksheet.Cell(1, 9).Value = "Ngân Hàng";
                worksheet.Cell(1, 10).Value = "Số Tài Khoản";

                // Ghi dữ liệu
                for (int i = 0; i < cancellations.Count; i++)
                {
                    var hp = cancellations[i];
                    worksheet.Cell(i + 2, 1).Value = hp.MaPHP;
                    worksheet.Cell(i + 2, 2).Value = hp.Ma_PDPhong;
                    worksheet.Cell(i + 2, 3).Value = hp.PhieuDatPhong?.NguoiDung?.FullName ?? "Không xác định";
                    worksheet.Cell(i + 2, 4).Value = hp.PhieuDatPhong?.ChiTietDatPhongs?.FirstOrDefault()?.Phong?.Homestay?.Ten_Homestay ?? "Không xác định";
                    worksheet.Cell(i + 2, 5).Value = hp.NgayHuy.ToString("dd/MM/yyyy");
                    worksheet.Cell(i + 2, 6).Value = hp.LyDo ?? "Không xác định";
                    worksheet.Cell(i + 2, 7).Value = hp.NguoiHuy ?? "Không xác định";
                    worksheet.Cell(i + 2, 8).Value = hp.TrangThai;
                    worksheet.Cell(i + 2, 9).Value = hp.TenNganHang ?? "Không xác định";
                    worksheet.Cell(i + 2, 10).Value = hp.SoTaiKhoan ?? "Không xác định";
                }

                // Tự động điều chỉnh độ rộng cột
                worksheet.Columns().AdjustToContents();

                // Xuất file
                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                var content = stream.ToArray();
                var fileName = $"Cancellations_{(exportOption == "all" ? "All" : "Pending")}_{DateTime.Now:yyyyMMdd}.xlsx";

                return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = "Không thể xuất Excel: " + ex.Message });
            }
        }
        [HttpPost("UpdateCancellationStatus")]
        public async Task<JsonResult> UpdateCancellationStatus([FromBody] UpdateCancellationStatusModel model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.MaPHP) || string.IsNullOrEmpty(model.TrangThai))
                {
                    return Json(new { success = false, message = "Mã phiếu hủy và trạng thái là bắt buộc" });
                }

                if (model.TrangThai != "Chờ xử lý" && model.TrangThai != "Đã xử lý")
                {
                    return Json(new { success = false, message = "Trạng thái không hợp lệ. Chỉ chấp nhận 'Chờ xử lý' hoặc 'Đã xử lý'" });
                }

                using var transaction = await _context.Database.BeginTransactionAsync();

                var cancellation = await _huyPhongRepo.GetByIdAsync(model.MaPHP);
                if (cancellation == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy phiếu hủy phòng" });
                }

                cancellation.TrangThai = model.TrangThai;
                await _huyPhongRepo.UpdateAsync(cancellation);
                await transaction.CommitAsync();

                return Json(new { success = true, message = "Cập nhật trạng thái phiếu hủy thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Không thể cập nhật trạng thái phiếu hủy: " + ex.Message });
            }
        }
        [HttpPost("CancelBooking")]
        public async Task<JsonResult> CancelBooking([FromBody] CancelBookingModel model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var phieuDatPhong = await _phieuDatPhongRepo.GetByIdAsync(model.Ma_PDPhong);
                if (phieuDatPhong == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy phiếu đặt phòng" });
                }

                if (phieuDatPhong.TrangThai == "Đã hủy")
                {
                    return Json(new { success = false, message = "Phiếu đã bị hủy trước đó" });
                }

                var chiTietDatPhongs = await _context.ChiTietDatPhongs
                    .Where(ct => ct.Ma_PDPhong == model.Ma_PDPhong)
                    .ToListAsync();
                if (!chiTietDatPhongs.Any())
                {
                    return Json(new { success = false, message = "Không tìm thấy chi tiết đặt phòng" });
                }

                var earliestCheckinDate = chiTietDatPhongs.Min(ct => ct.NgayDen);
                var cancelDeadline = earliestCheckinDate.AddDays(-3);
                if (DateTime.Now >= cancelDeadline)
                {
                    return Json(new { success = false, message = "Không thể hủy vì đã quá thời hạn (trước ngày nhận phòng 3 ngày)" });
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
                    NguoiHuy = "Admin",
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
using DoAnCs.Models;
using DoAnCs.Models.ViewModels;
using DoAnCs.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using DoAnCs.Serivces;
using DoAnCs.Models.Momo;
using DoAnCs.Services;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;

namespace DoAnCs.Controllers
{
    [Authorize]
    public class BookingController : Controller
    {
        private readonly IHomestayRepository _homestayRepository;
        private readonly IPhongRepository _phongRepository;
        private readonly IServiceRepository _serviceRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPhieuDatPhongRepository _phieuDatPhongRepository;
        private readonly ISelectedRoomsService _selectedRoomsService;
        private readonly IHoaDonRepository _hoaDonRepository;
        private readonly IThanhToanRepository _thanhToanRepository;
        private readonly IPhuThuRepository _phuThuService;
        private readonly IKhuyenMaiRepository _khuyenMaiRepository;
        private readonly IMoMoPaymentService _moMoPaymentService;
        private readonly IOptions<MoMoSettings> _moMoSettings;
        private readonly ILogger<BookingController> _logger;
        private readonly ApplicationDbContext _context;
        public BookingController(
            IHomestayRepository homestayRepository,
            IPhongRepository phongRepository,
            IServiceRepository serviceRepository,
            UserManager<ApplicationUser> userManager,
            IPhieuDatPhongRepository phieuDatPhongRepository,
            ISelectedRoomsService selectedRoomsService,
            IHoaDonRepository hoaDonRepository,
            IThanhToanRepository thanhToanRepository,
            IPhuThuRepository phuThuService,
            IKhuyenMaiRepository khuyenMaiRepository,
            IMoMoPaymentService moMoPaymentService,
            IOptions<MoMoSettings> moMoSettings,
            ILogger<BookingController> logger,
            ApplicationDbContext context)
        {
            _homestayRepository = homestayRepository;
            _phongRepository = phongRepository;
            _serviceRepository = serviceRepository;
            _userManager = userManager;
            _phieuDatPhongRepository = phieuDatPhongRepository;
            _selectedRoomsService = selectedRoomsService;
            _hoaDonRepository = hoaDonRepository;
            _thanhToanRepository = thanhToanRepository;
            _phuThuService = phuThuService;
            _khuyenMaiRepository = khuyenMaiRepository;
            _moMoPaymentService = moMoPaymentService;
            _moMoSettings = moMoSettings;
            _logger = logger;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Create(string maPhong, string checkInDate, string checkOutDate, string homestayId, string[] selectedRoomIds)
        {
            if (string.IsNullOrEmpty(homestayId) || string.IsNullOrEmpty(checkInDate) || string.IsNullOrEmpty(checkOutDate))
            {
                TempData["Error"] = "Thông tin homestay hoặc ngày không hợp lệ.";
                return RedirectToAction("Index", "Home");
            }

            if (!DateTime.TryParse(checkInDate, out var checkInDateParsed) ||
                !DateTime.TryParse(checkOutDate, out var checkOutDateParsed) ||
                checkInDateParsed < DateTime.Today ||
                checkOutDateParsed <= checkInDateParsed ||
                checkOutDateParsed > DateTime.Today.AddYears(1))
            {
                TempData["Error"] = "Ngày nhận phòng hoặc trả phòng không hợp lệ.";
                return RedirectToAction("Index", "Home");
            }

            var homestay = await _homestayRepository.GetByIdWithDetailsAsync(homestayId);
            if (homestay == null || homestay.TrangThai != "Hoạt động")
            {
                TempData["Error"] = "Homestay không tồn tại hoặc không hoạt động.";
                return NotFound();
            }

            var existingSelectedRoomIds = _selectedRoomsService.GetSelectedRoomIds(HttpContext) ?? new List<string>();
            bool isNewBooking = !string.IsNullOrEmpty(maPhong) && !existingSelectedRoomIds.Any() && (selectedRoomIds == null || !selectedRoomIds.Any());
            bool isAddingRoom = !string.IsNullOrEmpty(maPhong) && (existingSelectedRoomIds.Any() || (selectedRoomIds != null && selectedRoomIds.Any()));
            bool isReturning = string.IsNullOrEmpty(maPhong) && (existingSelectedRoomIds.Any() || (selectedRoomIds != null && selectedRoomIds.Any()));

            if (isNewBooking)
            {
                _selectedRoomsService.ClearSelectedRooms(HttpContext);
                if (await ValidateRoom(maPhong, checkInDateParsed, checkOutDateParsed))
                {
                    _selectedRoomsService.AddSelectedRoom(HttpContext, maPhong);
                }
                else
                {
                    TempData["Error"] = "Phòng không khả dụng. Vui lòng chọn phòng khác.";
                    return RedirectToAction("Details", "Homestay", new { id = homestayId, checkInDate, checkOutDate });
                }
            }
            else if (isAddingRoom)
            {
                if (await ValidateRoom(maPhong, checkInDateParsed, checkOutDateParsed))
                {
                    _selectedRoomsService.AddSelectedRoom(HttpContext, maPhong);
                }
                else
                {
                    TempData["Error"] = "Phòng không khả dụng. Vui lòng chọn phòng khác.";
                    return RedirectToAction("Details", "Homestay", new { id = homestayId, checkInDate, checkOutDate });
                }
            }
            else if (isReturning)
            {
                if (selectedRoomIds != null && selectedRoomIds.Any())
                {
                    foreach (var roomId in selectedRoomIds)
                    {
                        if (await ValidateRoom(roomId, checkInDateParsed, checkOutDateParsed))
                        {
                            _selectedRoomsService.AddSelectedRoom(HttpContext, roomId);
                        }
                    }
                }
            }

            existingSelectedRoomIds = _selectedRoomsService.GetSelectedRoomIds(HttpContext) ?? new List<string>();
            var selectedRooms = new List<SelectedRoom>();
            if (existingSelectedRoomIds.Any())
            {
                var rooms = await _phongRepository.GetByIdsAsync(existingSelectedRoomIds);
                foreach (var room in rooms)
                {
                    if (room != null &&
                        room.TrangThai == "Hoạt động" &&
                        !room.ChiTietDatPhongs.Any(ct => ct.PhieuDatPhong.TrangThai != "Đã hủy" && ct.NgayDen < checkOutDateParsed && ct.NgayDi > checkInDateParsed))
                    {
                        decimal totalPhuThu = await _phuThuService.CalculatePhuThuAsync(room.ID_Loai, checkInDateParsed, checkOutDateParsed, room.DonGia);
                        selectedRooms.Add(new SelectedRoom
                        {
                            MaPhong = room.Ma_Phong,
                            TenPhong = room.TenPhong,
                            DonGia = room.DonGia,
                            TotalPhuThu = totalPhuThu,
                            HinhAnh = room.HinhAnhPhongs?.FirstOrDefault(s => s.LaAnhChinh)?.UrlAnh ?? "",
                            SelectedServices = new List<SelectedService>()
                        });
                    }
                }
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["Error"] = "Vui lòng đăng nhập để đặt phòng.";
                return Unauthorized();
            }

            var model = new BookingViewModel
            {
                HomestayId = homestayId,
                HomestayName = homestay.Ten_Homestay,
                HomestayAddress = homestay.DiaChi,
                CheckInDate = checkInDateParsed,
                CheckOutDate = checkOutDateParsed,
                NumberOfNights = (checkOutDateParsed - checkInDateParsed).Days,
                SelectedRooms = selectedRooms
            };

            ViewBag.Services = await _serviceRepository.GetMinimalByHomestayAsync(homestayId);
            ViewBag.SelectedRoomIds = existingSelectedRoomIds.ToArray();
            ViewBag.User = user;
            return View(model);
        }

        private async Task<bool> ValidateRoom(string maPhong, DateTime checkInDate, DateTime checkOutDate)
        {
            if (string.IsNullOrEmpty(maPhong))
                return false;

            var room = await _phongRepository.GetByIdAsync(maPhong);
            return room != null &&
                   room.TrangThai == "Hoạt động" &&
                   !room.ChiTietDatPhongs.Any(ct => ct.PhieuDatPhong.TrangThai != "Đã hủy" && ct.NgayDen < checkOutDate && ct.NgayDi > checkInDate);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(string bookingData, string[] serviceIds, decimal[] serviceQuantities)
        {
            try
            {
                if (string.IsNullOrEmpty(bookingData))
                {
                    return Json(new { success = false, message = "Dữ liệu đặt phòng không hợp lệ." });
                }

                var model = JsonSerializer.Deserialize<BookingViewModel>(bookingData, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                if (model == null || !model.SelectedRooms.Any())
                {
                    return Json(new { success = false, message = "Không có phòng nào được chọn." });
                }

                var homestay = _homestayRepository.GetByIdWithDetailsAsync(model.HomestayId).Result;
                if (homestay == null || homestay.TrangThai != "Hoạt động")
                {
                    return Json(new { success = false, message = "Homestay không tồn tại hoặc không hoạt động." });
                }

                var user = _userManager.GetUserAsync(User).Result;
                if (user == null)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập lại." });
                }

                var services = _serviceRepository.GetMinimalByHomestayAsync(model.HomestayId).Result;
                foreach (var room in model.SelectedRooms)
                {
                    var dbRoom = _phongRepository.GetByIdAsync(room.MaPhong).Result;
                    if (dbRoom == null || dbRoom.TrangThai != "Hoạt động" ||
                        dbRoom.ChiTietDatPhongs.Any(ct => ct.PhieuDatPhong.TrangThai != "Đã hủy" && ct.NgayDen < model.CheckOutDate && ct.NgayDi > model.CheckInDate))
                    {
                        return Json(new { success = false, message = $"Phòng {room.TenPhong} không còn khả dụng." });
                    }

                    room.TotalPhuThu = _phuThuService.CalculatePhuThuAsync(dbRoom.ID_Loai, model.CheckInDate, model.CheckOutDate, dbRoom.DonGia).Result;
                    room.SelectedServices = serviceIds?.Select((id, index) => new { id, index })
                        .Where(s => s.index % model.SelectedRooms.Count == model.SelectedRooms.IndexOf(room))
                        .Select(s => new SelectedService
                        {
                            MaDV = s.id,
                            TenDV = services.FirstOrDefault(d => d.Ma_DV == s.id)?.Ten_DV ?? "Không xác định",
                            DonGia = services.FirstOrDefault(d => d.Ma_DV == s.id)?.DonGia ?? 0,
                            SoLuong = serviceQuantities != null && s.index < serviceQuantities.Length ? serviceQuantities[s.index] : 0
                        })
                        .Where(s => s.SoLuong > 0)
                        .ToList() ?? new List<SelectedService>();
                }

                var bookingDetails = new
                {
                    BookingViewModel = model,
                    User = new
                    {
                        user.Id,
                        user.UserName,
                        user.Email,
                        user.PhoneNumber,
                        FullName = user.FullName,
                        Address = user.Address
                    }
                };

                return Json(new { success = true, message = "Vui lòng xác nhận đặt phòng.", data = bookingDetails });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Create POST Error: " + ex.Message);
                return Json(new { success = false, message = "Đã xảy ra lỗi khi xử lý yêu cầu. Vui lòng thử lại.", error = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmBooking(string bookingData, string maKM)
        {
            try
            {
                if (string.IsNullOrEmpty(bookingData))
                {
                    return Json(new { success = false, message = "Dữ liệu đặt phòng không hợp lệ." });
                }

                var model = JsonSerializer.Deserialize<BookingViewModel>(bookingData, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                if (model == null || !model.SelectedRooms.Any())
                {
                    return Json(new { success = false, message = "Không có phòng nào được chọn." });
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập lại." });
                }

                foreach (var room in model.SelectedRooms)
                {
                    if (string.IsNullOrEmpty(room.MaPhong))
                    {
                        return Json(new { success = false, message = "Mã phòng không hợp lệ." });
                    }

                    foreach (var service in room.SelectedServices ?? new List<SelectedService>())
                    {
                        if (string.IsNullOrEmpty(service.MaDV))
                        {
                            return Json(new { success = false, message = "Mã dịch vụ không hợp lệ." });
                        }
                        if (service.SoLuong <= 0)
                        {
                            return Json(new { success = false, message = $"Số lượng dịch vụ {service.TenDV} phải lớn hơn 0." });
                        }
                    }
                }

                foreach (var room in model.SelectedRooms)
                {
                    var dbRoom = await _phongRepository.GetByIdAsync(room.MaPhong);
                    if (dbRoom == null)
                    {
                        return Json(new { success = false, message = $"Phòng {room.TenPhong} không tồn tại." });
                    }
                    if (dbRoom.TrangThai != "Hoạt động")
                    {
                        return Json(new { success = false, message = $"Phòng {room.TenPhong} không khả dụng." });
                    }
                    if (dbRoom.ChiTietDatPhongs != null && dbRoom.ChiTietDatPhongs.Any(ct => ct.PhieuDatPhong.TrangThai != "Đã hủy" && ct.NgayDen < model.CheckOutDate && ct.NgayDi > model.CheckInDate))
                    {
                        return Json(new { success = false, message = $"Phòng {room.TenPhong} đã được đặt trong khoảng thời gian này." });
                    }

                    room.TotalPhuThu = await _phuThuService.CalculatePhuThuAsync(dbRoom.ID_Loai, model.CheckInDate, model.CheckOutDate, dbRoom.DonGia);
                }

                var phieuDatPhong = new PhieuDatPhong
                {
                    Ma_PDPhong = "BK-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                    Ma_ND = user.Id,
                    NgayLap = DateTime.Now,
                    TrangThai = "Chờ xác nhận",
                    ChiTietDatPhongs = new List<ChiTietDatPhong>()
                };

                foreach (var room in model.SelectedRooms)
                {
                    var chiTietDatPhong = new ChiTietDatPhong
                    {
                        Ma_PDPhong = phieuDatPhong.Ma_PDPhong,
                        Ma_Phong = room.MaPhong,
                        NgayDen = model.CheckInDate,
                        NgayDi = model.CheckOutDate
                    };

                    if (room.SelectedServices?.Any() == true)
                    {
                        var phieuSuDungDV = new PhieuSuDungDV
                        {
                            Ma_Phieu = Guid.NewGuid().ToString(),
                            Ma_Phong = room.MaPhong,
                            Ma_PDPhong = phieuDatPhong.Ma_PDPhong,
                            ChiTietPhieuDVs = new List<ChiTietPhieuDV>()
                        };

                        var chiTietPhieuDVs = room.SelectedServices.Select(s => new ChiTietPhieuDV
                        {
                            Ma_Phieu = phieuSuDungDV.Ma_Phieu,
                            Ma_DV = s.MaDV,
                            SoLuong = s.SoLuong,
                            NgaySuDung = model.CheckInDate,
                            ID_Homestay = model.HomestayId
                        }).ToList();

                        phieuSuDungDV.ChiTietPhieuDVs = chiTietPhieuDVs;
                        chiTietDatPhong.PhieuSuDungDVs = new List<PhieuSuDungDV> { phieuSuDungDV };
                    }

                    phieuDatPhong.ChiTietDatPhongs.Add(chiTietDatPhong);
                }

                await _phieuDatPhongRepository.AddAsync(phieuDatPhong);

                decimal totalRoomPrice = model.SelectedRooms.Sum(r => r.DonGia * model.NumberOfNights);
                decimal totalPhuThu = model.SelectedRooms.Sum(r => r.TotalPhuThu);
                decimal totalServicePrice = model.SelectedRooms.Sum(r =>
                    r.SelectedServices?.Sum(s => s.DonGia * s.SoLuong * model.NumberOfNights) ?? 0);
                decimal totalAmountBeforeDiscount = totalRoomPrice + totalPhuThu ;

                decimal discount = 0;
                if (!string.IsNullOrEmpty(maKM))
                {
                    var promotion = await _khuyenMaiRepository.GetByIdAsync(maKM);
                    if (promotion != null && promotion.TrangThai == "Đang áp dụng")
                    {
                        var availablePromotions = await _khuyenMaiRepository.GetAvailableKhuyenMaiAsync(
                            model.HomestayId,
                            model.SelectedRooms.Select(r => r.MaPhong).ToList(),
                            user.Id,
                            model.NumberOfNights
                        );

                        if (availablePromotions.Any(km => km.Ma_KM == maKM))
                        {
                            if (promotion.LoaiChietKhau == "Percentage")
                            {
                                discount = totalAmountBeforeDiscount * (promotion.ChietKhau / 100);
                            }
                            else if (promotion.LoaiChietKhau == "Fixed")
                            {
                                discount = promotion.ChietKhau;
                            }
                        }
                    }
                }

               
                decimal totalAmount = totalAmountBeforeDiscount - discount + totalServicePrice;
                var maHD = "HD-" + DateTime.Now.ToString("yyyyMMddHHmmss");
                var hoaDon = new HoaDon
                {
                    Ma_HD = maHD,
                    TongTien = totalAmount,
                    NgayLap = DateTime.Now,
                    Thue = 0m,
                    TrangThai = "Chưa thanh toán",
                    Ma_ND = user.Id,
                    ChiTietHoaDons = new List<ChiTietHoaDon>
                    {
                        new ChiTietHoaDon
                        {
                            Ma_HD = maHD,
                            Ma_PDPhong = phieuDatPhong.Ma_PDPhong
                        }
                    }
                };

                if (!string.IsNullOrEmpty(maKM) && discount > 0)
                {
                    hoaDon.ApDungKMs = new List<ApDungKM>
                    {
                        new ApDungKM
                        {
                            Ma_HD = maHD,
                            Ma_KM = maKM
                        }
                    };
                }
               
                await _hoaDonRepository.AddAsync(hoaDon);

                var bookingDetails = new
                {
                    MaPDPhong = phieuDatPhong.Ma_PDPhong,
                    TrangThaiPDP = phieuDatPhong.TrangThai,
                    NgayLapPDP = phieuDatPhong.NgayLap.ToString("dd/MM/yyyy HH:mm"),
                    MaHD = hoaDon.Ma_HD,
                    TongTien = hoaDon.TongTien,
                    phiDatCoc = (totalRoomPrice + totalPhuThu - discount) * 0.15m,
                    Thue = hoaDon.Thue,
                    TotalPhuThu = totalPhuThu,
                    Discount = discount,
                    TrangThaiHD = hoaDon.TrangThai,
                    BookingViewModel = model,
                    User = new
                    {
                        user.Id,
                        user.UserName,
                        user.Email,
                        user.PhoneNumber,
                        FullName = user.FullName,
                        Address = user.Address
                    }
                };

                return Json(new { success = true, message = "Vui lòng tiến hành thanh toán.", data = bookingDetails });
            }
            catch (Exception ex)
            {
                Console.WriteLine("ConfirmBooking POST Error: " + ex.Message);
                return Json(new { success = false, message = "Đã xảy ra lỗi khi xác nhận đặt phòng. Vui lòng thử lại.", error = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult RemoveRoom([FromBody] string maPhong)
        {
            try
            {
                if (string.IsNullOrEmpty(maPhong))
                {
                    return Json(new { success = false, message = "Mã phòng không hợp lệ." });
                }

                var selectedRooms = _selectedRoomsService.GetSelectedRoomIds(HttpContext)?.ToList() ?? new List<string>();
                if (selectedRooms.Contains(maPhong))
                {
                    selectedRooms.Remove(maPhong);
                    _selectedRoomsService.ClearSelectedRooms(HttpContext);
                    foreach (var roomId in selectedRooms)
                    {
                        _selectedRoomsService.AddSelectedRoom(HttpContext, roomId);
                    }
                    return Json(new { success = true, message = "Đã xóa phòng khỏi danh sách." });
                }

                return Json(new { success = false, message = "Phòng không tồn tại trong danh sách đã chọn." });
            }
            catch (Exception ex)
            {
                Console.WriteLine("RemoveRoom Error: " + ex.Message);
                return Json(new { success = false, message = $"Đã xảy ra lỗi: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailablePromotions(string homestayId, string checkInDate, string checkOutDate, string[] selectedRoomIds)
        {
            try
            {
                if (string.IsNullOrEmpty(homestayId) || selectedRoomIds == null)
                {
                    return Json(new { success = false, message = "Thông tin đầu vào không hợp lệ." });
                }

                if (!DateTime.TryParse(checkInDate, out var checkInDateParsed) ||
                    !DateTime.TryParse(checkOutDate, out var checkOutDateParsed))
                {
                    return Json(new { success = false, message = "Định dạng ngày không hợp lệ." });
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập để lấy danh sách khuyến mãi." });
                }

                var numberOfNights = (checkOutDateParsed - checkInDateParsed).Days;
                var promotions = await _khuyenMaiRepository.GetAvailableKhuyenMaiAsync(
                    homestayId,
                    selectedRoomIds.ToList(),
                    user.Id,
                    numberOfNights
                );

                var promotionList = promotions.Select(km => new
                {
                    maKM = km.Ma_KM,
                    noiDung = km.NoiDung,
                    chietKhau = km.ChietKhau,
                    loaiChietKhau = km.LoaiChietKhau,
                    hsd = km.HSD.ToString("dd/MM/yyyy")
                }).ToList();

                return Json(new
                {
                    success = true,
                    data = promotionList
                }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetAvailablePromotions Error: {ex.Message}");
                return Json(new { success = false, message = "Đã xảy ra lỗi khi lấy danh sách khuyến mãi." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyPromotion(string maKM, string homestayId, string checkInDate, string checkOutDate, string[] selectedRoomIds)
        {
            try
            {
                if (string.IsNullOrEmpty(maKM) || string.IsNullOrEmpty(homestayId) || selectedRoomIds == null)
                {
                    return Json(new { success = false, message = "Thông tin đầu vào không hợp lệ." });
                }

                if (!DateTime.TryParse(checkInDate, out var checkInDateParsed) ||
                    !DateTime.TryParse(checkOutDate, out var checkOutDateParsed))
                {
                    return Json(new { success = false, message = "Định dạng ngày không hợp lệ." });
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập để áp dụng khuyến mãi." });
                }

                var numberOfNights = (checkOutDateParsed - checkInDateParsed).Days;
                var promotion = await _khuyenMaiRepository.GetByIdAsync(maKM);
                if (promotion == null || promotion.TrangThai != "Đang áp dụng")
                {
                    return Json(new { success = false, message = "Mã khuyến mãi không hợp lệ hoặc đã hết hạn." });
                }

                if (promotion.SoLuong > 0)
                {
                    var usedCount = await _khuyenMaiRepository.CountApDungKmAsync(maKM);
                    if (usedCount >= promotion.SoLuong)
                    {
                        return Json(new { success = false, message = "Mã khuyến mãi đã được sử dụng hết." });
                    }
                }

                var availablePromotions = await _khuyenMaiRepository.GetAvailableKhuyenMaiAsync(
                    homestayId,
                    selectedRoomIds.ToList(),
                    user.Id,
                    numberOfNights
                );

                if (!availablePromotions.Any(km => km.Ma_KM == maKM))
                {
                    return Json(new { success = false, message = "Mã khuyến mãi không áp dụng cho đặt phòng này." });
                }

                return Json(new
                {
                    success = true,
                    message = "Áp dụng khuyến mãi thành công.",
                    data = new
                    {
                        maKM = promotion.Ma_KM,
                        noiDung = promotion.NoiDung,
                        chietKhau = promotion.ChietKhau,
                        loaiChietKhau = promotion.LoaiChietKhau
                    }
                }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ApplyPromotion Error: {ex.Message}");
                return Json(new { success = false, message = "Đã xảy ra lỗi khi áp dụng khuyến mãi." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment(string maHD, string phuongThuc, decimal soTien)
        {
            try
            {
                var hoaDon = await _hoaDonRepository.GetByIdAsync(maHD);
                if (hoaDon == null || hoaDon.TrangThai == "Đã thanh toán")
                {
                    return Json(new { success = false, message = "Hóa đơn không hợp lệ hoặc đã thanh toán." });
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập lại." });
                }

                if (soTien <= 0 || soTien > hoaDon.TongTien)
                {
                    return Json(new { success = false, message = "Số tiền thanh toán không hợp lệ." });
                }

                if (phuongThuc == "MoMo")
                {
                    // Tạo yêu cầu thanh toán MoMo
                    var orderId = $"MOMO-{maHD}-{DateTime.Now:yyyyMMddHHmmss}";
                    var orderInfo = $"Thanh toán hóa đơn {maHD} cho {user.FullName}";
                    var amount = (long)soTien; // Chuyển sang VND, MoMo yêu cầu số nguyên

                    var paymentResponse = await _moMoPaymentService.CreatePaymentAsync(orderId, amount, orderInfo);

                    if (paymentResponse.ResultCode == 0)
                    {
                        // Lưu thông tin thanh toán tạm thời (trạng thái chờ)
                        var thanhToan = new ThanhToan
                        {
                            MaTT = $"TT-{DateTime.Now:yyyyMMddHHmmss}",
                            MaHD = maHD,
                            SoTien = soTien,
                            PhuongThuc = phuongThuc,
                            NgayTT = DateTime.Now,
                            TrangThai = "Chờ xử lý",
                            NoiDung = $"Thanh toán hóa đơn {maHD} qua MoMo, OrderId: {orderId}"
                        };
                        await _thanhToanRepository.AddAsync(thanhToan);

                        return Json(new { success = true, redirectUrl = paymentResponse.PayUrl });
                    }
                    else
                    {
                        return Json(new { success = false, message = $"Lỗi từ MoMo: {paymentResponse.Message}" });
                    }
                }
                else
                {
                  
                    var thanhToan = new ThanhToan
                    {
                        MaTT = $"TT-{DateTime.Now:yyyyMMddHHmmss}",
                        MaHD = maHD,
                        SoTien = soTien,
                        PhuongThuc = phuongThuc,
                        NgayTT = DateTime.Now,
                        TrangThai = "Thành công",
                        NoiDung = $"Thanh toán hóa đơn {maHD} bằng {phuongThuc}"
                    };
                    await _thanhToanRepository.AddAsync(thanhToan);

                    await _hoaDonRepository.UpdateStatusAsync(hoaDon.Ma_HD, "Đã thanh toán");
                    var chiTietHoaDon = hoaDon.ChiTietHoaDons.FirstOrDefault();
                    if (chiTietHoaDon != null) {
                        var phieuDatPhong = await _phieuDatPhongRepository.GetByIdAsync(chiTietHoaDon.Ma_PDPhong);
                        if (phieuDatPhong != null)
                        {
                            phieuDatPhong.TrangThai = "Đã xác nhận";
                            await _phieuDatPhongRepository.UpdateAsync(phieuDatPhong);
                        }
                    }
                    

                    var paymentDetails = new
                    {
                        MaTT = thanhToan.MaTT,
                        MaHD = thanhToan.MaHD,
                        SoTien = thanhToan.SoTien,
                        PhuongThuc = thanhToan.PhuongThuc,
                        NgayTT = thanhToan.NgayTT.ToString("dd/MM/yyyy HH:mm"),
                        TrangThai = thanhToan.TrangThai,
                        NoiDung = thanhToan.NoiDung
                    };

                    return Json(new { success = true, message = "Thanh toán thành công.", data = paymentDetails });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ProcessPayment Error: {ex.Message}");
                return Json(new { success = false, message = "Đã xảy ra lỗi khi xử lý thanh toán. Vui lòng thử lại." + ex.ToString(), error = ex.Message });
            }
        }

        [HttpGet]
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> MoMoCallback([FromQuery][FromBody] MoMoCallback callback)
        {
            try
            {
                _logger.LogInformation("Nhận callback MoMo: OrderId={OrderId}, ResultCode={ResultCode}, TransId={TransId}, Method={Method}, CallbackData={CallbackData}",
                    callback.OrderId, callback.ResultCode, callback.TransId, HttpContext.Request.Method, JsonSerializer.Serialize(callback));

                // Xử lý yêu cầu POST không hợp lệ
                if (HttpContext.Request.Method == "POST" && string.IsNullOrWhiteSpace(callback.OrderId))
                {
                    _logger.LogInformation("Bỏ qua yêu cầu POST không hợp lệ từ MoMo: CallbackData={CallbackData}",
                        JsonSerializer.Serialize(callback));
                    return Json(new { success = false, message = "Yêu cầu POST không hợp lệ từ MoMo." });
                }

                // Kiểm tra OrderId và Signature
                if (string.IsNullOrWhiteSpace(callback.OrderId) || string.IsNullOrWhiteSpace(callback.Signature))
                {
                    _logger.LogWarning("OrderId hoặc Signature rỗng: OrderId={OrderId}, Signature={Signature}",
                        callback.OrderId, callback.Signature);
                    return Json(new { success = false, message = "OrderId hoặc Signature không hợp lệ." });
                }
                // Kiểm tra và tách OrderId
                var orderIdParts = callback.OrderId.Split('-');
                if (orderIdParts.Length != 4)
                {
                    _logger.LogWarning("OrderId không đúng định dạng: OrderId={OrderId}, Parts={Parts}",
                        callback.OrderId, string.Join(",", orderIdParts));
                    return Json(new { success = false, message = "OrderId không đúng định dạng (kỳ vọng: MOMO-HD-YYYYMMDDHHMMSS-timestamp)." });
                }
                if (string.IsNullOrWhiteSpace(orderIdParts[1]) || string.IsNullOrWhiteSpace(orderIdParts[2]))
                {
                    _logger.LogWarning("Phần tử OrderId không hợp lệ: OrderId={OrderId}, HD={HD}, Timestamp={Timestamp}",
                        callback.OrderId, orderIdParts[1], orderIdParts[2]);
                    return Json(new { success = false, message = "Phần tử OrderId không hợp lệ." });
                }

                var maHD = $"{orderIdParts[1].Trim()}-{orderIdParts[2].Trim()}";
                _logger.LogInformation("Mã hóa đơn: MaHD={MaHD}, OriginalOrderId={OrderId}", maHD, callback.OrderId);

                // Kiểm tra hóa đơn
                var hoaDon = await _hoaDonRepository.GetByIdAsync(maHD);
                if (hoaDon == null)
                {
                    _logger.LogWarning("Hóa đơn không tồn tại: MaHD={MaHD}", maHD);
                    var allHoaDons = await _context.HoaDons.Select(h => h.Ma_HD).ToListAsync();
                    _logger.LogInformation("Danh sách Ma_HD trong database: {MaHDs}", string.Join(",", allHoaDons));
                    return Json(new { success = false, message = $"Hóa đơn với MaHD={maHD} không tồn tại trong hệ thống." });
                }
                _logger.LogInformation("Tìm thấy hóa đơn: MaHD={MaHD}, TongTien={TongTien}, TrangThai={TrangThai}",
                    maHD, hoaDon.TongTien, hoaDon.TrangThai);

                // Kiểm tra kết quả thanh toán
                if (callback.ResultCode == 0)
                {
                    var thanhToan = await _thanhToanRepository.GetByMaHDAsync(maHD);
                    if (thanhToan == null)
                    {
                        _logger.LogWarning("Không tìm thấy thanh toán: MaHD={MaHD}", maHD);
                        return Json(new { success = false, message = "Không tìm thấy thông tin thanh toán." });
                    }
                    if (thanhToan.TrangThai != "Chờ xử lý")
                    {
                        _logger.LogWarning("Thanh toán không ở trạng thái Chờ xử lý: MaHD={MaHD}, TrangThai={TrangThai}",
                            maHD, thanhToan.TrangThai);
                        return Json(new { success = false, message = $"Thanh toán không ở trạng thái Chờ xử lý (hiện tại: {thanhToan.TrangThai})." });
                    }

                    // Dùng transaction để đảm bảo cập nhật nhất quán
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        // Cập nhật thanh toán
                        thanhToan.TrangThai = "Thành công";
                        thanhToan.HoaDon.TrangThai = "Đã thanh toán";
                        thanhToan.HoaDon.ChiTietHoaDons.FirstOrDefault().PhieuDatPhong.TrangThai = "Đã xác nhận";
                        thanhToan.NoiDung += $", TransId: {callback.TransId}";
                        await _thanhToanRepository.UpdateAsync(thanhToan);
                        _logger.LogInformation("Cập nhật thanh toán thành công: MaHD={MaHD}, TrangThai={TrangThai}",
                            maHD, thanhToan.TrangThai);

                      
                            await _hoaDonRepository.UpdateStatusAsync(hoaDon.Ma_HD, "Đã thanh toán");
                            _logger.LogInformation("Cập nhật hóa đơn: MaHD={MaHD}, TrangThai=Đã thanh toán", maHD);

                            var chiTietHoaDon = hoaDon.ChiTietHoaDons?.FirstOrDefault();
                            if (chiTietHoaDon != null)
                            {
                                var phieuDatPhong = await _phieuDatPhongRepository.GetByIdAsync(chiTietHoaDon.Ma_PDPhong);
                                if (phieuDatPhong != null)
                                {
                                    phieuDatPhong.TrangThai = "Đã xác nhận";
                                    await _phieuDatPhongRepository.UpdateAsync(phieuDatPhong);
                                    _logger.LogInformation("Cập nhật phiếu đặt phòng: MaPDPhong={MaPDPhong}, TrangThai=Đã xác nhận",
                                        chiTietHoaDon.Ma_PDPhong);
                                }
                                else
                                {
                                    _logger.LogWarning("Không tìm thấy phiếu đặt phòng: MaPDPhong={MaPDPhong}",
                                        chiTietHoaDon.Ma_PDPhong);
                                }
                            }
                            else
                            {
                                _logger.LogWarning("Không tìm thấy chi tiết hóa đơn: MaHD={MaHD}", maHD);
                            }
                      

                        await transaction.CommitAsync();
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, "Lỗi cập nhật trạng thái: MaHD={MaHD}", maHD);
                        throw;
                    }

                    // Xử lý GET (ReturnUrl) hoặc POST (NotifyUrl)
                    if (HttpContext.Request.Method == "GET")
                    {
                        TempData["Success"] = "Thanh toán MoMo thành công!";
                        _logger.LogInformation("Chuyển hướng về trang chủ: OrderId={OrderId}", callback.OrderId);
                        return RedirectToAction("Index", "Home");
                    }
                    _logger.LogInformation("Xử lý NotifyUrl thành công: OrderId={OrderId}", callback.OrderId);
                    return Json(new { success = true, message = "Xử lý thanh toán MoMo thành công." });
                }
                else
                {
                    _logger.LogWarning("Thanh toán MoMo thất bại: OrderId={OrderId}, ResultCode={ResultCode}, Message={Message}",
                        callback.OrderId, callback.ResultCode, callback.Message);
                    if (HttpContext.Request.Method == "GET")
                    {
                        TempData["Error"] = $"Thanh toán MoMo thất bại: {callback.Message}";
                        return RedirectToAction("Index", "Home");
                    }
                    return Json(new { success = false, message = $"Thanh toán MoMo thất bại: {callback.Message}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xử lý callback MoMo: OrderId={OrderId}", callback.OrderId);
                return Json(new { success = false, message = "Lỗi khi xử lý callback từ MoMo.", error = ex.Message });
            }
        }
    }
}
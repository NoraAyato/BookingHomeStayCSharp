using DoAnCs.Models;
using DoAnCs.Models.ViewModels;
using DoAnCs.Repository;
using DoAnCs.Serivces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DoAnCs.Controllers
{
    public class HomestayController : Controller
    {
        private readonly IHomestayRepository _homestayRepository;
        private readonly IPhongRepository _phongRepository;
        private readonly IServiceRepository _serviceRepository;
        private readonly IDanhGiaRepository _danhGiaRepository;
        private readonly IConfiguration _configuration;
        private readonly ISelectedRoomsService _selectedRoomsService;
        private readonly ILogger<HomestayController> _logger;
        private readonly IPhuThuRepository _phieuPhuThuRepository;
        private readonly IChinhSachRepository _chinhSachRepository;
        private readonly IKhuVucRepository _khuVucRepository;
        private readonly IKhuyenMaiRepository _khuyenMaiRepo;
        private readonly IPhuThuRepository _phuThuRepository;

        public HomestayController(
            IHomestayRepository homestayRepository,
            IPhongRepository phongRepository,
            IServiceRepository serviceRepository,
            IDanhGiaRepository danhGiaRepository,
            IConfiguration configuration,
            ISelectedRoomsService selectedRoomsService,
            ILogger<HomestayController> logger,
            IPhuThuRepository phieuPhuThuRepository,
            IChinhSachRepository chinhSachRepository,
            IKhuVucRepository khuVucRepository,
            IKhuyenMaiRepository khuyenMaiRepo,
            IPhuThuRepository phuThuRepository)
        {
            _homestayRepository = homestayRepository;
            _phongRepository = phongRepository;
            _serviceRepository = serviceRepository;
            _danhGiaRepository = danhGiaRepository;
            _configuration = configuration;
            _selectedRoomsService = selectedRoomsService;
            _logger = logger;
            _phieuPhuThuRepository = phieuPhuThuRepository;
            _chinhSachRepository = chinhSachRepository;
            _khuVucRepository = khuVucRepository;
            _khuyenMaiRepo = khuyenMaiRepo;
            _phuThuRepository = phuThuRepository;
        }
        [HttpGet]
        public async Task<IActionResult> GetKhuVucs()
        {
            try
            {
                var khuVucs = await _khuVucRepository.GetAllAsync();
                var khuVucList = khuVucs.Select(k => new { k.Ma_KV, k.Ten_KV }).ToList();
                return Json(new { success = true, data = khuVucList });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi lấy danh sách khu vực: " + ex.Message });
            }
        }
        [HttpGet]
        public async Task<IActionResult> Index(string khuVucName = null)
        {
            try
            {
                var khuVucs = await _khuVucRepository.GetAllAsync();
                string locationFilter = "all";

                if (!string.IsNullOrEmpty(khuVucName))
                {
                    var khuVuc = await _khuVucRepository.GetByNameAsync(khuVucName);
                    if (khuVuc != null)
                    {
                        locationFilter = khuVuc.Ma_KV;
                    }
                    else
                    {
                        locationFilter = "all";
                    }
                }

                var model = new HomestayViewModel
                {
                    KhuVucs = khuVucs,
                    SearchString = "",
                    LocationFilter = locationFilter,
                    PriceFilter = null,
                    RatingFilter = null,
                    SortOrder = "name_asc",
                    CurrentPage = 1,
                    TotalPages = 1
                };
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Index action with khuVucName={0}", khuVucName);
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public async Task<IActionResult> SearchHomestays(string searchString, string locationFilter, decimal? priceFilter, decimal? ratingFilter, string sortOrder = "name_asc", int pageNumber = 1)
        {
            try
            {
                _logger.LogInformation("SearchHomestays called with searchString={0}, locationFilter={1}, priceFilter={2}, ratingFilter={3}, sortOrder={4}, pageNumber={5}",
                    searchString, locationFilter, priceFilter, ratingFilter, sortOrder, pageNumber);

                const int pageSize = 9;
                var query = _homestayRepository.GetAllQueryable();
                query = query.Where(h => h.TrangThai == "Hoạt động" && h.Phongs.Count()>0);
                if (!string.IsNullOrEmpty(searchString))
                {
                    query = query.Where(h => h.Ten_Homestay.Contains(searchString) || h.DiaChi.Contains(searchString));
                }

                if (!string.IsNullOrEmpty(locationFilter) && locationFilter != "all")
                {
                    query = query.Where(h => h.Ma_KV == locationFilter);
                }

                if (priceFilter.HasValue)
                {
                    query = query.Where(h => h.PricePerNight >= priceFilter.Value);
                }

                if (ratingFilter.HasValue)
                {
                    query = query.Where(h => h.Hang.HasValue && h.Hang >= ratingFilter.Value);
                }

                query = sortOrder switch
                {
                    "name_desc" => query.OrderByDescending(h => h.Ten_Homestay),
                    "price_asc" => query.OrderBy(h => h.PricePerNight),
                    "price_desc" => query.OrderByDescending(h => h.PricePerNight),
                    "date_asc" => query.OrderBy(h => h.NgayTao),
                    "date_desc" => query.OrderByDescending(h => h.NgayTao),
                    "rating_desc" => query.OrderByDescending(h => h.Hang),
                    "rating_asc" => query.OrderBy(h => h.Hang),
                    _ => query.OrderBy(h => h.Ten_Homestay),
                };

                var totalHomestays = await query.CountAsync();
                var homestays = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(h => new
                    {
                        IdHomestay = h.ID_Homestay,
                        TenHomestay = h.Ten_Homestay,
                        KhuVuc = h.KhuVuc != null ? new { MaKv = h.KhuVuc.Ma_KV, TenKv = h.KhuVuc.Ten_KV } : null,
                        DiaChi = h.DiaChi,
                        PricePerNight = h.PricePerNight,
                        HinhAnh = h.HinhAnh,
                        Hang = h.Hang
                    })
                    .ToListAsync();

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    ReferenceHandler = ReferenceHandler.IgnoreCycles
                };

                return Json(new
                {
                    success = true,
                    data = homestays,
                    currentPage = pageNumber,
                    totalPages = (int)Math.Ceiling(totalHomestays / (double)pageSize)
                }, jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SearchHomestays");
                return Json(new { success = false, message = "Lỗi khi tìm kiếm homestay" });
            }
        }
        public IActionResult HomestayDetails(string id)
        {
            ViewBag.HomestayId = id;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetHomestayDetails(string id, string checkInDate = null, string checkOutDate = null)
        {
            try
            {
                // Kiểm tra ID homestay
                if (string.IsNullOrEmpty(id))
                {
                    _logger.LogWarning("Homestay ID is null or empty in GetHomestayDetails");
                    return BadRequest(new { success = false, message = "ID Homestay không hợp lệ" });
                }

                // Kiểm tra ngày nhận/trả phòng nếu có
                DateTime? checkInDateParsed = null;
                DateTime? checkOutDateParsed = null;
                if (!string.IsNullOrEmpty(checkInDate) && !string.IsNullOrEmpty(checkOutDate))
                {
                    if (!DateTime.TryParse(checkInDate, out var checkIn) ||
                        !DateTime.TryParse(checkOutDate, out var checkOut) ||
                        checkIn < DateTime.Today ||
                        checkOut <= checkIn ||
                        checkOut > DateTime.Today.AddYears(1))
                    {
                        _logger.LogWarning("Invalid dates: checkInDate={0}, checkOutDate={1}", checkInDate, checkOutDate);
                        return BadRequest(new { success = false, message = "Ngày nhận phòng hoặc trả phòng không hợp lệ" });
                    }
                    checkInDateParsed = checkIn;
                    checkOutDateParsed = checkOut;
                }

                // Lấy thông tin homestay
                var homestay = await _homestayRepository.GetByIdWithDetailsAsync(id);
                if (homestay == null || homestay.TrangThai != "Hoạt động")
                {
                    _logger.LogWarning("Homestay not found or inactive: id={0}", id);
                    return NotFound(new { success = false, message = "Không tìm thấy homestay hoặc homestay không hoạt động" });
                }

                // Lấy danh sách phòng
                var rooms = await _phongRepository.GetByHomestayAsync(id);
                var availableRooms = new List<object>();
                foreach (var room in rooms)
                {
                    if (room.TrangThai != "Hoạt động") continue;

                    // Kiểm tra phòng trống nếu có ngày nhận/trả phòng
                    if (checkInDateParsed.HasValue && checkOutDateParsed.HasValue &&
                        room.ChiTietDatPhongs.Any(ct => ct.NgayDen < checkOutDateParsed && ct.NgayDi > checkInDateParsed))
                    {
                        continue;
                    }

                    // Lấy tiện nghi và hình ảnh
                    var tienNghis = await _phongRepository.GetChiTietPhongsAsync(room.Ma_Phong);
                    var hinhAnhs = await _phongRepository.GetHinhAnhPhongsAsync(room.Ma_Phong);

                    // Tính phụ thu nếu có ngày
                    decimal totalPhuThu = 0;
                    if (checkInDateParsed.HasValue && checkOutDateParsed.HasValue)
                    {
                        totalPhuThu = await _phieuPhuThuRepository.CalculatePhuThuAsync(
                            room.LoaiPhong?.ID_Loai,
                            checkInDateParsed.Value,
                            checkOutDateParsed.Value,
                            room.DonGia);
                    }

                    availableRooms.Add(new
                    {
                        MaPhong = room.Ma_Phong,
                        TenPhong = room.TenPhong,
                        DonGia = room.DonGia,
                        SoNguoi = room.SoNguoi,
                        TenLoai = room.LoaiPhong?.TenLoai ?? "Không xác định",
                        TienNghis = tienNghis.Select(t => new
                        {
                            MaTienNghi = t.Ma_TienNghi,
                            TenTienNghi = t.TienNghi?.TenTienNghi,
                            SoLuong = t.SoLuong
                        }).ToList(),
                        VirtualTours = room.VirtualTour ?? null,
                        HinhAnhs = hinhAnhs.Select(h => new
                        {
                            Id = h.Id,
                            UrlAnh = h.UrlAnh,
                            MoTa = h.MoTa,
                            LaAnhChinh = h.LaAnhChinh
                        }).ToList(),
                        TotalPhuThu = totalPhuThu
                    });
                }

                // Tạo dữ liệu trả về
                var homestayData = new
                {
                    IdHomestay = homestay.ID_Homestay,
                    TenHomestay = homestay.Ten_Homestay,
                    MaKV = homestay.Ma_KV,
                    TenKV = homestay.KhuVuc?.Ten_KV ?? "Không xác định",
                    DiaChi = homestay.DiaChi,
                    PricePerNight = homestay.PricePerNight,
                    HinhAnh = homestay.HinhAnh,
                    Hang = homestay.Hang,
                    TrangThai = homestay.TrangThai,
                    MaND = homestay.Ma_ND,
                    Rooms = availableRooms
                };

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    ReferenceHandler = ReferenceHandler.IgnoreCycles
                };

                return Json(new
                {
                    Success = true,
                    Data = homestayData
                }, jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetHomestayDetails for id={0}, checkInDate={1}, checkOutDate={2}", id, checkInDate, checkOutDate);
                return StatusCode(500, new { Success = false, Message = "Lỗi khi lấy thông tin homestay" });
            }
        }

        

        [HttpGet]
        public async Task<IActionResult> GetAllKhuVuc()
        {
            try
            {
                _logger.LogInformation("GetAllKhuVuc called");

                var khuVucs = await _khuVucRepository.GetAllAsync();
                var khuVucData = khuVucs.Select(k => new
                {
                    MaKv = k.Ma_KV,
                    TenKv = k.Ten_KV
                }).ToList();

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    ReferenceHandler = ReferenceHandler.IgnoreCycles
                };

                return Json(new
                {
                    success = true,
                    data = khuVucData
                }, jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllKhuVuc");
                return Json(new { success = false, message = "Lỗi khi lấy danh sách khu vực" });
            }
        }
        [HttpPost]
        public IActionResult ClearSelectedRooms()
        {
            try
            {
                _selectedRoomsService.ClearSelectedRooms(HttpContext);
                _logger.LogInformation("Cleared selected rooms via ClearSelectedRooms action.");
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while clearing selected rooms.");
                return Json(new { success = false, message = "Lỗi trong quá trình thực hiện" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id, string checkInDate, string checkOutDate, string location, string[] selectedRoomIds)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    _logger.LogWarning("Homestay ID is null or empty");
                    return NotFound();
                }

                if (!DateTime.TryParse(checkInDate, out var checkInDateParsed) ||
                    !DateTime.TryParse(checkOutDate, out var checkOutDateParsed) ||
                    checkInDateParsed < DateTime.Today ||
                    checkOutDateParsed <= checkInDateParsed ||
                    checkOutDateParsed > DateTime.Today.AddYears(1))
                {
                    _logger.LogWarning("Invalid dates: checkInDate={0}, checkOutDate={1}", checkInDate, checkOutDate);
                    return RedirectToAction("Index", "Home");
                }

                var homestay = await _homestayRepository.GetByIdWithDetailsAsync(id);
                if (homestay == null || homestay.TrangThai != "Hoạt động")
                {
                    _logger.LogWarning("Homestay not found or inactive: id={0}", id);
                    return NotFound();
                }

                var existingSelectedRoomIds = _selectedRoomsService.GetSelectedRoomIds(HttpContext);
                _logger.LogInformation("Current selectedRoomIds from Session: {0}", string.Join(", ", existingSelectedRoomIds));

                if (selectedRoomIds != null && selectedRoomIds.Any())
                {
                    foreach (var roomId in selectedRoomIds)
                    {
                        _selectedRoomsService.AddSelectedRoom(HttpContext, roomId);
                    }
                    existingSelectedRoomIds = _selectedRoomsService.GetSelectedRoomIds(HttpContext);
                }

                var rooms = await _phongRepository.GetByHomestayAsync(id);
                var availableRooms = new List<PhongDetailsViewModel>();

                var roomIds = rooms.Select(r => r.Ma_Phong).ToList();
                var allTienNghis = new Dictionary<string, List<ChiTietPhong>>();
                var allHinhAnhs = new Dictionary<string, List<HinhAnhPhong>>();

                foreach (var roomId in roomIds)
                {
                    var tienNghis = await _phongRepository.GetChiTietPhongsAsync(roomId);
                    var hinhAnhs = await _phongRepository.GetHinhAnhPhongsAsync(roomId);
                    allTienNghis[roomId] = tienNghis.ToList();
                    allHinhAnhs[roomId] = hinhAnhs.ToList();
                }

                foreach (var room in rooms)
                {
                    if (room.TrangThai == "Hoạt động" &&
                        !room.ChiTietDatPhongs.Any(ct => ct.PhieuDatPhong.TrangThai != "Đã hủy" &&
                            ct.NgayDen < checkOutDateParsed &&
                            ct.NgayDi > checkInDateParsed) &&
                        !existingSelectedRoomIds.Contains(room.Ma_Phong))
                    {
                        decimal totalPhuThu = _phieuPhuThuRepository.CalculatePhuThuAsync(
                            room.LoaiPhong?.ID_Loai,
                            checkInDateParsed,
                            checkOutDateParsed,
                            room.DonGia).Result;
                        availableRooms.Add(new PhongDetailsViewModel
                        {
                            Ma_Phong = room.Ma_Phong,
                            TenPhong = room.TenPhong,
                            DonGia = room.DonGia,
                            SoNguoi = (int)room.SoNguoi,
                            TenLoai = room.LoaiPhong?.TenLoai ?? "Không xác định",
                            TienNghis = allTienNghis[room.Ma_Phong],
                            HinhAnhs = allHinhAnhs[room.Ma_Phong].Where(h => h.LaAnhChinh).ToList(),
                            TotalPhuThu = totalPhuThu,
                        });
                    }
                }

                var model = new HomestayDetailsViewModel
                {
                    Homestay = homestay,
                    AvailableRooms = availableRooms,
                    CheckInDate = checkInDateParsed,
                    CheckOutDate = checkOutDateParsed,
                    NumberOfNights = (checkOutDateParsed - checkInDateParsed).Days,
                    HostEmail = homestay.NguoiDung?.Email ?? "N/A",
                    HostPhone = homestay.NguoiDung?.PhoneNumber ?? "N/A"
                };

                ViewBag.GoogleMapsApiKey = _configuration["GoogleMaps:ApiKey"];
                ViewBag.Location = location;
                ViewBag.SelectedRoomIds = existingSelectedRoomIds.ToArray();

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Details: id={0}, checkInDate={1}, checkOutDate={2}", id, checkInDate, checkOutDate);
                return RedirectToAction("Index", "Home");
            }
        }
        [HttpGet]
        public IActionResult GetGoogleMapsApiKey()
        {
            try
            {
                var apiKey = _configuration["GoogleMaps:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogWarning("Google Maps API Key is not configured");
                    return Json(new { success = false, message = "API Key không được cấu hình" });
                }

                return Json(new { success = true, data = apiKey });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Google Maps API Key");
                return StatusCode(500, new { success = false, message = "Lỗi khi lấy API Key" });
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetDanhGiaByHomestayId(string homestayId)
        {
            try
            {
                if (string.IsNullOrEmpty(homestayId))
                {
                    return BadRequest(new { success = false, message = "ID homestay không hợp lệ." });
                }

                var danhGias = await _danhGiaRepository.SearchAsync(null, homestayId, null, null, null, null);

                var result = danhGias.Select(dg => new
                {
                    MaND = dg.Ma_ND,
                    TenNguoiDung = dg.NguoiDung?.FullName ?? "Ẩn danh",
                    BinhLuan = dg.BinhLuan,
                    NgayDanhGia = dg.NgayDanhGia,
                    Rating = dg.Rating,
                    HinhAnh = dg.HinhAnh
                });

                return Json(new { success = true, danhGias = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách đánh giá cho homestayId={0}", homestayId);
                return StatusCode(500, new { success = false, message = "Lỗi server khi lấy đánh giá." });
            }
        }
        
        [HttpGet]
        public async Task<IActionResult> GetDichVusByHomestay(string idHomestay)
        {
            try
            {
                if (string.IsNullOrEmpty(idHomestay))
                {
                    _logger.LogWarning("Homestay ID is null or empty in GetDichVusByHomestay");
                    return BadRequest(new { success = false, message = "ID Homestay không hợp lệ" });
                }

                var dichVus = await _serviceRepository.GetMinimalByHomestayAsync(idHomestay);
                var dichVuData = dichVus.Select(dv => new
                {
                    maDv = dv.Ma_DV,
                    tenDv = dv.Ten_DV,
                    donGia = dv.DonGia
                }).ToList();

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    ReferenceHandler = ReferenceHandler.IgnoreCycles
                };

                return Json(new
                {
                    success = true,
                    data = dichVuData
                }, jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetDichVusByHomestay for idHomestay={0}", idHomestay);
                return StatusCode(500, new { success = false, message = "Lỗi khi lấy danh sách dịch vụ" });
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetChinhSach(string homestayId)
        {
            try
            {
                if (string.IsNullOrEmpty(homestayId))
                {
                    return BadRequest(new { success = false, message = "Homestay ID không hợp lệ" });
                }

                var chinhSach = await _chinhSachRepository.GetByHomestayIdAsync(homestayId);
                if (chinhSach == null)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy chính sách cho homestay này" });
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        nhanPhong = chinhSach.NhanPhong,
                        traPhong = chinhSach.TraPhong,
                        buaAn = chinhSach.BuaAn,
                        huyPhong = chinhSach.HuyPhong
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy chính sách: homestayId={0}", homestayId);
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi lấy chính sách" });
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetPopularAreas()
        {
            var popularAreas = await _khuVucRepository.GetPopularAreasAsync();

            var result = popularAreas.Select(area => new
            {
                tenKv = area.Ten_KV,
                maKv = area.Ma_KV,
                bookingCount = area.BookingCount
            });

            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetPopularHomestays()
        {
           
                var homestays = await _homestayRepository.GetPopularHomestaysAsync();
                if (homestays == null)
                {
                    _logger.LogWarning("Homestays list is null");
                    return Json(new { success = false, message = "Không tìm thấy homestay phổ biến" });
                }
                _logger.LogInformation($"Retrieved {homestays.Count()} homestays");

                var result = new List<HomestayCardView>();

                foreach (var h in homestays)
                {
                    _logger.LogInformation($"Processing homestay ID: {h.Id}, Name: {h.Name}");
                    if (h.OriginalPrice == null)
                    {
                        _logger.LogWarning($"Homestay {h.Id} has null OriginalPrice");
                        continue; // Bỏ qua homestay này hoặc gán giá mặc định
                    }

                    _logger.LogInformation($"Fetching promotion for homestay ID: {h.Id}");
                    var chietKhau = await _khuyenMaiRepo.GetHighestPromotionByHomestay(h.Id);

                    decimal price = (decimal)h.OriginalPrice;
                    decimal? oldPrice = chietKhau > 0 ? price : null;
                    decimal newPrice = Math.Max(0, chietKhau > 0 ? price * (1 - chietKhau / 100.0m) : price);


                result.Add(new HomestayCardView
                    {
                        Id = h.Id,
                        Name = h.Name,
                        Image = h.Image,
                        Address = h.Address,
                        Rating = (double)(h.Rank ?? 4.5m),
                        DiscountPercent = chietKhau > 0 ? chietKhau : null,
                        OriginalPrice = oldPrice,
                        Price = newPrice
                    });
                }

                _logger.LogInformation($"Returning {result.Count} homestays");
                return Json(result);
            }
        [HttpGet]
        public async Task<IActionResult> GetBookedDatesForRoom(string maPhong)
        {
            try
            {
                if (string.IsNullOrEmpty(maPhong))
                {
                    _logger.LogWarning("Room ID is null or empty in GetBookedDatesForRoom");
                    return BadRequest(new { success = false, message = "Mã phòng không hợp lệ" });
                }

                var bookedDates = await _phongRepository.GetBookedDateRangesAsync(maPhong);
                var result = bookedDates.Select(d => new
                {
                    NgayDen = d.NgayDen.ToString("yyyy-MM-dd"),
                    NgayDi = d.NgayDi.ToString("yyyy-MM-dd")
                }).ToList();

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetBookedDatesForRoom for maPhong={0}", maPhong);
                return StatusCode(500, new { success = false, message = "Lỗi khi lấy khoảng thời gian đã đặt" });
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetPhuThu(string maPhong, DateTime checkInDate, DateTime checkOutDate)
        {
            try
            {
                if (string.IsNullOrEmpty(maPhong))
                {
                    _logger.LogWarning("Mã phòng không được cung cấp trong GetPhuThu");
                    return BadRequest(new { success = false, message = "Mã phòng là bắt buộc" });
                }

                if (checkInDate < DateTime.Today || checkOutDate <= checkInDate || checkOutDate > DateTime.Today.AddYears(1))
                {
                    _logger.LogWarning("Ngày nhận/trả phòng không hợp lệ: checkInDate={CheckInDate}, checkOutDate={CheckOutDate}", checkInDate, checkOutDate);
                    return BadRequest(new { success = false, message = "Ngày nhận hoặc trả phòng không hợp lệ" });
                }

                // Lấy thông tin phòng
                var phong = await _phongRepository.GetByIdAsync(maPhong);
                if (phong == null)
                {
                    _logger.LogWarning("Không tìm thấy phòng với mã: {MaPhong}", maPhong);
                    return NotFound(new { success = false, message = "Không tìm thấy phòng" });
                }

                // Tính phụ thu
                decimal phuThu = await _phuThuRepository.CalculatePhuThuAsync(phong.ID_Loai, checkInDate, checkOutDate, phong.DonGia);
                _logger.LogInformation("Tính phụ thu cho phòng {MaPhong}: {PhuThu} VNĐ", maPhong, phuThu);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        maPhong,
                        phuThu = phuThu
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tính phụ thu cho phòng {MaPhong}", maPhong);
                return StatusCode(500, new { success = false, message = "Lỗi server khi tính phụ thu" });
            }
        }
    }
}
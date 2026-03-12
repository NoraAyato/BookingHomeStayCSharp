using DoAnCs.Models;
using DoAnCs.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;

namespace DoAnCs.Areas.Host.Controllers
{
    [Area("Host")]
    [Authorize(Roles = "Host")]
    public class PromotionController : Controller
    {
        private readonly IKhuyenMaiRepository _khuyenMaiRepo;
        private readonly IHomestayRepository _homestayRepo;
        private readonly IPhongRepository _phongRepo;
        private readonly ILogger<PromotionController> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public PromotionController(
            IKhuyenMaiRepository khuyenMaiRepo,
            IHomestayRepository homestayRepo,
            IPhongRepository phongRepo,
            ILogger<PromotionController> logger,
            IWebHostEnvironment webHostEnvironment)
        {
            _khuyenMaiRepo = khuyenMaiRepo;
            _homestayRepo = homestayRepo;
            _phongRepo = phongRepo;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetPromotions(string searchString, string status, string dateRange, int page = 1, int pageSize = 10)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("GetPromotions: UserId not found");
                    return Json(new { success = false, message = "Không tìm thấy thông tin người dùng" });
                }

                var homestays = await _homestayRepo.GetHomestaysByOwnerAsync(userId);
                var homestayIds = homestays.Select(h => h.ID_Homestay).ToList();

                if (!homestayIds.Any())
                {
                    _logger.LogWarning($"GetPromotions: No homestays found for user {userId}");
                    return Json(new { success = true, data = new object[] { }, totalPages = 0, currentPage = page, totalItems = 0 });
                }

                var query = _khuyenMaiRepo.GetAllQueryable()
                    .Where(p => p.TrangThai == "Đang áp dụng" &&
                                p.HSD >= DateTime.Now &&
                                (p.ApDungChoTatCaPhong ||
                                 p.KhuyenMaiPhongs.Any(ph => homestayIds.Contains(ph.ID_Homestay))));

                if (!string.IsNullOrEmpty(searchString))
                {
                    searchString = searchString.ToLower();
                    query = query.Where(p => p.NoiDung.ToLower().Contains(searchString));
                }

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(p => p.TrangThai == status);
                }

                if (!string.IsNullOrEmpty(dateRange))
                {
                    _logger.LogInformation($"Received dateRange: {dateRange}");
                    var dates = dateRange.Split(" to ");
                    if (dates.Length == 2 &&
                        DateTime.TryParseExact(dates[0], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var startDate) &&
                        DateTime.TryParseExact(dates[1], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var endDate))
                    {
                        startDate = startDate.Date;
                        endDate = endDate.Date.AddDays(1).AddTicks(-1);
                        _logger.LogInformation($"Parsed startDate: {startDate:yyyy-MM-dd}, endDate: {endDate:yyyy-MM-dd}");
                        query = query.Where(p => p.HSD.Date >= startDate && p.HSD.Date <= endDate);
                    }
                    else
                    {
                        _logger.LogWarning($"Invalid dateRange format: {dateRange}. Expected format: YYYY-MM-DD to YYYY-MM-DD");
                    }
                }

                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
                var promotions = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new
                    {
                        p.Ma_KM,
                        p.NoiDung,
                        p.NgayBatDau,
                        p.HSD,
                        p.ChietKhau,
                        p.LoaiChietKhau,
                        p.SoDemToiThieu,
                        p.SoNgayDatTruoc,
                        p.ChiApDungChoKhachMoi,
                        p.ApDungChoTatCaPhong,
                        p.TrangThai,
                        p.SoLuong,
                        p.NguoiTaoId,
                        IsEditable = p.NguoiTaoId == userId,
                        Phongs = p.KhuyenMaiPhongs.Select(ph => new { ph.Ma_Phong, ph.ID_Homestay }).ToList()
                    })
                    .ToListAsync();

                _logger.LogInformation($"Returning {promotions.Count} promotions for host {userId}.");
                return Json(new
                {
                    success = true,
                    data = promotions,
                    totalPages,
                    currentPage = page,
                    totalItems
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPromotions");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPromotion(string id)
        {
            try
            {
                var promotion = await _khuyenMaiRepo.GetByIdAsync(id);
                if (promotion == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy khuyến mãi" });
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return Json(new
                {
                    success = true,
                    maKM = promotion.Ma_KM,
                    noiDung = promotion.NoiDung,
                    ngayBatDau = promotion.NgayBatDau.ToString("yyyy-MM-dd"),
                    hsd = promotion.HSD.ToString("yyyy-MM-dd"),
                    chietKhau = promotion.ChietKhau,
                    loaiChietKhau = promotion.LoaiChietKhau,
                    soDemToiThieu = promotion.SoDemToiThieu,
                    soNgayDatTruoc = promotion.SoNgayDatTruoc,
                    chiApDungChoKhachMoi = promotion.ChiApDungChoKhachMoi,
                    apDungChoTatCaPhong = promotion.ApDungChoTatCaPhong,
                    trangThai = promotion.TrangThai,
                    soLuong = promotion.SoLuong,
                    nguoiTaoId = promotion.NguoiTaoId,
                    hinhAnh = promotion.HinhAnh,
                    isEditable = promotion.NguoiTaoId == userId,
                    phongs = promotion.KhuyenMaiPhongs.Select(ph => new { ph.Ma_Phong, ph.ID_Homestay }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPromotion");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRoomsByOwner()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("GetRoomsByOwner: UserId not found");
                    return Json(new { success = false, message = "Không tìm thấy thông tin người dùng" });
                }

                var homestays = await _homestayRepo.GetHomestaysByOwnerAsync(userId);
                var homestayIds = homestays.Select(h => h.ID_Homestay).ToList();

                if (!homestayIds.Any())
                {
                    _logger.LogWarning($"GetRoomsByOwner: No homestays found for user {userId}");
                    return Json(new { success = true, data = new object[] { } });
                }

                var rooms = new List<Phong>();
                foreach (var homestayId in homestayIds)
                {
                    var homestayRooms = await _phongRepo.GetByHomestayAsync(homestayId);
                    rooms.AddRange(homestayRooms);
                }

                var roomData = rooms.Select(r => new
                {
                    maPhong = r.Ma_Phong,
                    tenPhong = r.TenPhong,
                    idHomestay = r.ID_Homestay,
                    tenHomestay = homestays.FirstOrDefault(h => h.ID_Homestay == r.ID_Homestay)?.Ten_Homestay
                }).ToList();

                return Json(new { success = true, data = roomData });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetRoomsByOwner");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] KhuyenMai model, IFormFile HinhAnhFile)
        {
            try
            {
                _logger.LogInformation("Create: Received promotion data: {@Model}", model);

                // Kiểm tra Ma_KM
                if (string.IsNullOrWhiteSpace(model.Ma_KM))
                {
                    _logger.LogWarning("Create: Ma_KM is null or empty");
                    return Json(new { success = false, message = "Mã khuyến mãi không được để trống" });
                }

                if (model.Ma_KM.Length > 20)
                {
                    _logger.LogWarning("Create: Ma_KM exceeds 20 characters: {Ma_KM}", model.Ma_KM);
                    return Json(new { success = false, message = "Mã khuyến mãi không được dài quá 20 ký tự" });
                }

                if (!Regex.IsMatch(model.Ma_KM, @"^[a-zA-Z0-9\-]+$"))
                {
                    _logger.LogWarning("Create: Invalid Ma_KM format: {Ma_KM}", model.Ma_KM);
                    return Json(new { success = false, message = "Mã khuyến mãi chỉ được chứa chữ cái, số và dấu gạch ngang" });
                }

                var existingPromotion = await _khuyenMaiRepo.GetByIdAsync(model.Ma_KM);
                if (existingPromotion != null)
                {
                    _logger.LogWarning("Create: Ma_KM already exists: {Ma_KM}", model.Ma_KM);
                    return Json(new { success = false, message = "Mã khuyến mãi đã tồn tại" });
                }

                // Xác thực ModelState
                ModelState.Remove("NguoiTao");
                ModelState.Remove("NguoiTaoId");
                ModelState.Remove("KhuyenMaiPhongs");
                ModelState.Remove("ApDungKMs");
                ModelState.Remove("HinhAnh");
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    _logger.LogWarning("Create: Invalid model state: {Errors}", string.Join(", ", errors));
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ: " + string.Join(", ", errors) });
                }

                // Kiểm tra chiết khấu
                if (model.LoaiChietKhau == "Percentage" && (model.ChietKhau < 1 || model.ChietKhau > 100))
                {
                    _logger.LogWarning("Create: Invalid percentage discount: {ChietKhau}", model.ChietKhau);
                    return Json(new { success = false, message = "Chiết khấu phần trăm phải từ 1% đến 100%" });
                }
                else if (model.LoaiChietKhau == "Fixed" && model.ChietKhau <= 0)
                {
                    _logger.LogWarning("Create: Invalid fixed discount: {ChietKhau}", model.ChietKhau);
                    return Json(new { success = false, message = "Chiết khấu cố định phải lớn hơn 0" });
                }

                // Kiểm tra số lượng
                if (model.SoLuong < 0)
                {
                    _logger.LogWarning("Create: Invalid quantity: {SoLuong}", model.SoLuong);
                    return Json(new { success = false, message = "Số lượng phải lớn hơn hoặc bằng 0" });
                }

                // Kiểm tra ngày
                if (model.NgayBatDau.Date < DateTime.Now.Date)
                {
                    _logger.LogWarning("Create: Invalid start date: {NgayBatDau}", model.NgayBatDau);
                    return Json(new { success = false, message = "Ngày bắt đầu không được nhỏ hơn ngày hiện tại" });
                }

                if (model.HSD.Date < model.NgayBatDau.Date)
                {
                    _logger.LogWarning("Create: Invalid end date: {HSD} < {NgayBatDau}", model.HSD, model.NgayBatDau);
                    return Json(new { success = false, message = "HSD không được nhỏ hơn ngày bắt đầu" });
                }

                // Xử lý hình ảnh
                string hinhAnhPath = null;
                if (HinhAnhFile != null)
                {
                    var validImageTypes = new[] { "image/jpeg", "image/png", "image/gif" };
                    if (!validImageTypes.Contains(HinhAnhFile.ContentType))
                    {
                        _logger.LogWarning("Create: Invalid image format for HinhAnhFile: {ContentType}", HinhAnhFile.ContentType);
                        return Json(new { success = false, message = "Chỉ hỗ trợ định dạng .jpg, .jpeg, .png, .gif" });
                    }

                    if (HinhAnhFile.Length > 5 * 1024 * 1024) // 5MB
                    {
                        _logger.LogWarning("Create: Image file size exceeds 5MB: {FileSize} bytes", HinhAnhFile.Length);
                        return Json(new { success = false, message = "Kích thước hình ảnh không được vượt quá 5MB" });
                    }

                    var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "img", "promotions");
                    try
                    {
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                            _logger.LogInformation("Create: Created uploads directory at {UploadsFolder}", uploadsFolder);
                        }

                        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(HinhAnhFile.FileName)}";
                        var filePath = Path.Combine(uploadsFolder, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await HinhAnhFile.CopyToAsync(stream);
                        }

                        hinhAnhPath = $"/img/promotions/{fileName}";
                        _logger.LogInformation("Create: Image saved at {HinhAnhPath}", hinhAnhPath);
                    }
                    catch (IOException ex)
                    {
                        _logger.LogError(ex, "Create: Failed to save image to {UploadsFolder}", uploadsFolder);
                        return Json(new { success = false, message = "Lỗi khi lưu hình ảnh. Vui lòng thử lại." });
                    }
                }

                // Lấy danh sách homestay và phòng của host
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var homestays = await _homestayRepo.GetHomestaysByOwnerAsync(userId);
                var homestayIds = homestays.Select(h => h.ID_Homestay).ToList();

                if (!homestayIds.Any())
                {
                    return Json(new { success = false, message = "Bạn chưa có homestay để áp dụng khuyến mãi" });
                }

                var rooms = new List<Phong>();
                foreach (var homestayId in homestayIds)
                {
                    var homestayRooms = await _phongRepo.GetByHomestayAsync(homestayId);
                    rooms.AddRange(homestayRooms);
                }

                if (!rooms.Any())
                {
                    return Json(new { success = false, message = "Bạn chưa có phòng nào để áp dụng khuyến mãi" });
                }

                // Gán đường dẫn hình ảnh và các thuộc tính khác vào model
                model.HinhAnh = hinhAnhPath;
                model.NguoiTaoId = userId;
                model.NgayTao = DateTime.Now;

                // Xử lý danh sách phòng áp dụng
                if (model.ApDungChoTatCaPhong)
                {
                    model.KhuyenMaiPhongs = rooms.Select(r => new KhuyenMaiPhong
                    {
                        Ma_KM = model.Ma_KM,
                        Ma_Phong = r.Ma_Phong,
                        ID_Homestay = r.ID_Homestay
                    }).ToList();
                }
                else if (model.KhuyenMaiPhongs != null && model.KhuyenMaiPhongs.Any())
                {
                    var validRoomIds = rooms.Select(r => r.Ma_Phong).ToList();
                    var invalidRooms = model.KhuyenMaiPhongs.Select(p => p.Ma_Phong).Except(validRoomIds).ToList();

                    if (invalidRooms.Any())
                    {
                        _logger.LogWarning("Create: Invalid room IDs provided: {InvalidRooms}", string.Join(", ", invalidRooms));
                        return Json(new { success = false, message = "Một hoặc nhiều phòng không hợp lệ hoặc không thuộc homestay của bạn" });
                    }
                }
                else
                {
                    _logger.LogWarning("Create: No rooms selected when ApDungChoTatCaPhong is false");
                    return Json(new { success = false, message = "Vui lòng chọn ít nhất một phòng hoặc chọn áp dụng cho tất cả phòng" });
                }
                model.ApDungChoTatCaPhong = false;
                // Lưu khuyến mãi
                await _khuyenMaiRepo.AddAsync(model);
                _logger.LogInformation("Create: Successfully created promotion with ID {Ma_KM} by host {UserId}", model.Ma_KM, userId);
                return Json(new { success = true, message = "Thêm khuyến mãi thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Create");
                return Json(new { success = false, message = "Lỗi khi thêm khuyến mãi: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update([FromForm] KhuyenMai model, IFormFile HinhAnhFile)
        {
            try
            {
                _logger.LogInformation("Update: Received promotion data: {@Model}", model);

                var existingPromotion = await _khuyenMaiRepo.GetByIdAsync(model.Ma_KM);
                if (existingPromotion == null)
                {
                    _logger.LogWarning("Update: Promotion with ID {Ma_KM} not found", model.Ma_KM);
                    return Json(new { success = false, message = "Không tìm thấy khuyến mãi" });
                }

                // Kiểm tra quyền chỉnh sửa
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var homestays = await _homestayRepo.GetHomestaysByOwnerAsync(userId);
                var homestayIds = homestays.Select(h => h.ID_Homestay).ToList();

                if (existingPromotion.ApDungChoTatCaPhong)
                {
                    return Json(new { success = false, message = "Bạn không có quyền chỉnh sửa khuyến mãi này" });
                }

                if (existingPromotion.NguoiTaoId != userId)
                {
                    return Json(new { success = false, message = "Bạn không có quyền chỉnh sửa khuyến mãi do admin tạo" });
                }

                // Xác thực ModelState
                ModelState.Remove("NguoiTao");
                ModelState.Remove("NguoiTaoId");
                ModelState.Remove("KhuyenMaiPhongs");
                ModelState.Remove("ApDungKMs");
                ModelState.Remove("HinhAnh");
                ModelState.Remove("Ma_KM");
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    _logger.LogWarning("Update: Invalid model state: {Errors}", string.Join(", ", errors));
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ: " + string.Join(", ", errors) });
                }

                // Kiểm tra chiết khấu
                if (model.LoaiChietKhau == "Percentage" && (model.ChietKhau < 1 || model.ChietKhau > 100))
                {
                    _logger.LogWarning("Update: Invalid percentage discount: {ChietKhau}", model.ChietKhau);
                    return Json(new { success = false, message = "Chiết khấu phần trăm phải từ 1% đến 100%" });
                }
                else if (model.LoaiChietKhau == "Fixed" && model.ChietKhau <= 0)
                {
                    _logger.LogWarning("Update: Invalid fixed discount: {ChietKhau}", model.ChietKhau);
                    return Json(new { success = false, message = "Chiết khấu cố định phải lớn hơn 0" });
                }

                // Kiểm tra số lượng
                if (model.SoLuong < 0)
                {
                    _logger.LogWarning("Update: Invalid quantity: {SoLuong}", model.SoLuong);
                    return Json(new { success = false, message = "Số lượng phải lớn hơn hoặc bằng 0" });
                }

                // Kiểm tra ngày
                if (model.NgayBatDau.Date < DateTime.Now.Date)
                {
                    _logger.LogWarning("Update: Invalid start date: {NgayBatDau}", model.NgayBatDau);
                    return Json(new { success = false, message = "Ngày bắt đầu không được nhỏ hơn ngày hiện tại" });
                }

                if (model.HSD.Date < model.NgayBatDau.Date)
                {
                    _logger.LogWarning("Update: Invalid end date: {HSD} < {NgayBatDau}", model.HSD, model.NgayBatDau);
                    return Json(new { success = false, message = "HSD không được nhỏ hơn ngày bắt đầu" });
                }

                // Xử lý hình ảnh
                string hinhAnhPath = existingPromotion.HinhAnh;
                if (HinhAnhFile != null)
                {
                    var validImageTypes = new[] { "image/jpeg", "image/png", "image/gif" };
                    if (!validImageTypes.Contains(HinhAnhFile.ContentType))
                    {
                        _logger.LogWarning("Update: Invalid image format for HinhAnhFile: {ContentType}", HinhAnhFile.ContentType);
                        return Json(new { success = false, message = "Chỉ hỗ trợ định dạng .jpg, .jpeg, .png, .gif" });
                    }

                    if (HinhAnhFile.Length > 5 * 1024 * 1024) // 5MB
                    {
                        _logger.LogWarning("Update: Image file size exceeds 5MB: {FileSize} bytes", HinhAnhFile.Length);
                        return Json(new { success = false, message = "Kích thước hình ảnh không được vượt quá 5MB" });
                    }

                    // Lưu hình ảnh mới
                    var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "promotions");
                    try
                    {
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                            _logger.LogInformation("Update: Created uploads directory at {UploadsFolder}", uploadsFolder);
                        }

                        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(HinhAnhFile.FileName)}";
                        var filePath = Path.Combine(uploadsFolder, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await HinhAnhFile.CopyToAsync(stream);
                        }

                        hinhAnhPath = $"/img/promotions/{fileName}";
                        _logger.LogInformation("Update: New image saved at {HinhAnhPath}", hinhAnhPath);
                    }
                    catch (IOException ex)
                    {
                        _logger.LogError(ex, "Update: Failed to save image to {UploadsFolder}", uploadsFolder);
                        return Json(new { success = false, message = "Lỗi khi lưu hình ảnh. Vui lòng thử lại." });
                    }
                }

                // Lấy danh sách phòng
                var rooms = new List<Phong>();
                foreach (var homestayId in homestayIds)
                {
                    var homestayRooms = await _phongRepo.GetByHomestayAsync(homestayId);
                    rooms.AddRange(homestayRooms);
                }

                if (!rooms.Any())
                {
                    return Json(new { success = false, message = "Bạn chưa có phòng nào để áp dụng khuyến mãi" });
                }

                // Cập nhật khuyến mãi
                existingPromotion.NoiDung = model.NoiDung;
                existingPromotion.NgayBatDau = model.NgayBatDau;
                existingPromotion.HSD = model.HSD;
                existingPromotion.ChietKhau = model.ChietKhau;
                existingPromotion.LoaiChietKhau = model.LoaiChietKhau;
                existingPromotion.SoDemToiThieu = model.SoDemToiThieu;
                existingPromotion.SoNgayDatTruoc = model.SoNgayDatTruoc;
                existingPromotion.ChiApDungChoKhachMoi = model.ChiApDungChoKhachMoi;
                existingPromotion.ApDungChoTatCaPhong = model.ApDungChoTatCaPhong;
                existingPromotion.TrangThai = model.TrangThai;
                existingPromotion.SoLuong = model.SoLuong;
                existingPromotion.HinhAnh = hinhAnhPath;

                // Xử lý danh sách phòng áp dụng
                if (model.ApDungChoTatCaPhong)
                {
                    existingPromotion.KhuyenMaiPhongs = rooms.Select(r => new KhuyenMaiPhong
                    {
                        Ma_KM = model.Ma_KM,
                        Ma_Phong = r.Ma_Phong,
                        ID_Homestay = r.ID_Homestay
                    }).ToList();
                }
                else if (model.KhuyenMaiPhongs != null && model.KhuyenMaiPhongs.Any())
                {
                    var validRoomIds = rooms.Select(r => r.Ma_Phong).ToList();
                    var invalidRooms = model.KhuyenMaiPhongs.Select(p => p.Ma_Phong).Except(validRoomIds).ToList();

                    if (invalidRooms.Any())
                    {
                        _logger.LogWarning("Update: Invalid room IDs provided: {InvalidRooms}", string.Join(", ", invalidRooms));
                        return Json(new { success = false, message = "Một hoặc nhiều phòng không hợp lệ hoặc không thuộc homestay của bạn" });
                    }

                    existingPromotion.KhuyenMaiPhongs = model.KhuyenMaiPhongs;
                }
                else
                {
                    _logger.LogWarning("Update: No rooms selected when ApDungChoTatCaPhong is false");
                    return Json(new { success = false, message = "Vui lòng chọn ít nhất một phòng hoặc chọn áp dụng cho tất cả phòng" });
                }

                // Lưu cập nhật
                await _khuyenMaiRepo.UpdateAsync(existingPromotion);
                _logger.LogInformation("Update: Successfully updated promotion with ID {Ma_KM} by host {UserId}", model.Ma_KM, userId);
                return Json(new { success = true, message = "Cập nhật khuyến mãi thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Update");
                return Json(new { success = false, message = "Lỗi khi cập nhật khuyến mãi: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("DeleteConfirmed")]
        public async Task<IActionResult> DeleteConfirmed([FromBody] string id)
        {
            try
            {
                var promotion = await _khuyenMaiRepo.GetByIdAsync(id);
                if (promotion == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy khuyến mãi" });
                }
                if (promotion.ApDungKMs.Count() > 0 && DateTime.Now <= promotion.HSD)
                {
                    return Json(new { success = false, message = "Khuyến mãi này đã được áp dụng cho khách hàng và không thể xóa" });
                }
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var homestays = await _homestayRepo.GetHomestaysByOwnerAsync(userId);
                var homestayIds = homestays.Select(h => h.ID_Homestay).ToList();

                if (promotion.ApDungChoTatCaPhong)
                {
                    return Json(new { success = false, message = "Bạn không có quyền xóa khuyến mãi này" });
                }

                if (promotion.NguoiTaoId != userId)
                {
                    return Json(new { success = false, message = "Bạn không có quyền xóa khuyến mãi do admin tạo" });
                }
                await _khuyenMaiRepo.DeleteAsync(id);
                _logger.LogInformation("DeleteConfirmed: Successfully deleted promotion with ID {Id} by host {UserId}", id, userId);
                return Json(new { success = true, message = "Xóa khuyến mãi thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteConfirmed");
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
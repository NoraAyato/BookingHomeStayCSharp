using DoAnCs.Models;
using DoAnCs.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Globalization;

namespace DoAnCs.Areas.Admin.Controllers
{
    public class PromotionController : BaseController
    {
        private readonly IKhuyenMaiRepository _khuyenMaiRepo;
        private readonly ILogger<PromotionController> _logger;

        public PromotionController(IKhuyenMaiRepository khuyenMaiRepo, ILogger<PromotionController> logger)
        {
            _khuyenMaiRepo = khuyenMaiRepo;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetPromotions(string searchString, string status, string dateRange, string homestay, int page = 1, int pageSize = 10)
        {
            try
            {
                var query = _khuyenMaiRepo.GetAllQueryable();

                // Lọc theo tìm kiếm
                if (!string.IsNullOrEmpty(searchString))
                {
                    searchString = searchString.ToLower();
                    query = query.Where(p => p.NoiDung.ToLower().Contains(searchString));
                }

                // Lọc theo trạng thái
                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(p => p.TrangThai == status);
                }

                // Lọc theo khoảng HSD
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

                // Lọc theo homestay
                if (!string.IsNullOrEmpty(homestay))
                {
                    _logger.LogInformation($"Filtering by homestay: {homestay}");
                    query = query.Where(p =>
                       p.KhuyenMaiPhongs.Any(ph=> ph.Phong.Homestay.ID_Homestay== homestay) // Hoặc có trong danh sách homestay cụ thể
                    );
                }

                // Tính toán phân trang
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
                     
                        Phongs = p.KhuyenMaiPhongs.Select(ph => new { ph.Ma_Phong, ph.ID_Homestay }).ToList()
                    })
                    .ToListAsync();

                _logger.LogInformation($"Returning {promotions.Count} promotions.");

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
                    hinhAnh = promotion.HinhAnh,
                    nguoiTaoId = promotion.NguoiTaoId,
                 
                    phongs = promotion.KhuyenMaiPhongs.Select(ph => new { ph.Ma_Phong, ph.ID_Homestay }).ToList()
                });
            }
            catch (Exception ex)
            {
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

                // Validation cho Ma_KM
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

                // Kiểm tra trùng Ma_KM
                var existingPromotion = await _khuyenMaiRepo.GetByIdAsync(model.Ma_KM);
                if (existingPromotion != null)
                {
                    _logger.LogWarning("Create: Ma_KM already exists: {Ma_KM}", model.Ma_KM);
                    return Json(new { success = false, message = "Mã khuyến mãi đã tồn tại" });
                }

                // Validation các trường khác
                ModelState.Remove("NguoiTaoId");
                ModelState.Remove("NguoiTao");
                ModelState.Remove("KhuyenMaiHomestays");
                ModelState.Remove("KhuyenMaiPhongs");
                ModelState.Remove("ApDungKMs");
                ModelState.Remove("HinhAnh"); // Bỏ qua HinhAnh vì sử dụng HinhAnhFile
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

                if (model.SoLuong < 0)
                {
                    _logger.LogWarning("Create: Invalid quantity: {SoLuong}", model.SoLuong);
                    return Json(new { success = false, message = "Số lượng phải lớn hơn hoặc bằng 0" });
                }

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

                // Xử lý tải lên hình ảnh
                if (HinhAnhFile != null && HinhAnhFile.Length > 0)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var fileExtension = Path.GetExtension(HinhAnhFile.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        _logger.LogWarning("Create: Invalid image file extension: {FileExtension}", fileExtension);
                        return Json(new { success = false, message = "Chỉ hỗ trợ các định dạng hình ảnh: .jpg, .jpeg, .png, .gif" });
                    }

                    var uploadsDir = Path.Combine("wwwroot", "img", "promotions");
                    if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);

                    var fileName = $"{Guid.NewGuid()}{fileExtension}";
                    var filePath = Path.Combine(uploadsDir, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await HinhAnhFile.CopyToAsync(stream);
                    }

                    model.HinhAnh = $"/img/promotions/{fileName}";
                }
                else
                {
                    model.HinhAnh = null; // Không có hình ảnh được tải lên
                }

                // Validation cho HinhAnh
                if (!string.IsNullOrEmpty(model.HinhAnh) && model.HinhAnh.Length > 200)
                {
                    _logger.LogWarning("Create: HinhAnh exceeds 200 characters: {HinhAnh}", model.HinhAnh);
                    return Json(new { success = false, message = "Đường dẫn hình ảnh không được vượt quá 200 ký tự" });
                }

                // Gán giá trị mặc định
                model.NguoiTaoId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                model.NgayTao = DateTime.Now;
                model.ApDungChoTatCaPhong = true;
                model.KhuyenMaiPhongs = null; // Không áp dụng phòng cụ thể

                await _khuyenMaiRepo.AddAsync(model);
                _logger.LogInformation("Create: Successfully created promotion with ID {Ma_KM}", model.Ma_KM);
                return Json(new { success = true, message = "Thêm khuyến mãi thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Create");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update([FromForm] KhuyenMai model, IFormFile HinhAnhFile)
        {
            try
            {
                _logger.LogInformation("Update: Received promotion data: {@Model}", model);

             

                // Kiểm tra sự tồn tại của khuyến mãi
                var existingPromotion = await _khuyenMaiRepo.GetByIdAsync(model.Ma_KM);
                if (existingPromotion == null)
                {
                    _logger.LogWarning("Update: Promotion with ID {Ma_KM} not found", model.Ma_KM);
                    return Json(new { success = false, message = "Không tìm thấy khuyến mãi" });
                }

                // Validation các trường khác
                ModelState.Remove("NguoiTaoId");
                ModelState.Remove("NguoiTao");
                ModelState.Remove("KhuyenMaiHomestays");
                ModelState.Remove("KhuyenMaiPhongs");
                ModelState.Remove("ApDungKMs");
                ModelState.Remove("Ma_KM");
                ModelState.Remove("HinhAnh"); // Bỏ qua HinhAnh vì sử dụng HinhAnhFile
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

                if (model.SoLuong < 0)
                {
                    _logger.LogWarning("Update: Invalid quantity: {SoLuong}", model.SoLuong);
                    return Json(new { success = false, message = "Số lượng phải lớn hơn hoặc bằng 0" });
                }

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

                // Xử lý tải lên hình ảnh
                if (HinhAnhFile != null && HinhAnhFile.Length > 0)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var fileExtension = Path.GetExtension(HinhAnhFile.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        _logger.LogWarning("Update: Invalid image file extension: {FileExtension}", fileExtension);
                        return Json(new { success = false, message = "Chỉ hỗ trợ các định dạng hình ảnh: .jpg, .jpeg, .png, .gif" });
                    }

                    var uploadsDir = Path.Combine("wwwroot", "img", "promotions");
                    if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);

                    var fileName = $"{Guid.NewGuid()}{fileExtension}";
                    var filePath = Path.Combine(uploadsDir, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await HinhAnhFile.CopyToAsync(stream);
                    }

                    existingPromotion.HinhAnh = $"/img/promotions/{fileName}";
                }

                // Validation cho HinhAnh
                if (!string.IsNullOrEmpty(existingPromotion.HinhAnh) && existingPromotion.HinhAnh.Length > 200)
                {
                    _logger.LogWarning("Update: HinhAnh exceeds 200 characters: {HinhAnh}", existingPromotion.HinhAnh);
                    return Json(new { success = false, message = "Đường dẫn hình ảnh không được vượt quá 200 ký tự" });
                }

                // Cập nhật các giá trị
                existingPromotion.NoiDung = model.NoiDung;
                existingPromotion.NgayBatDau = model.NgayBatDau;
                existingPromotion.HSD = model.HSD;
                existingPromotion.ChietKhau = model.ChietKhau;
                existingPromotion.LoaiChietKhau = model.LoaiChietKhau;
                existingPromotion.SoDemToiThieu = model.SoDemToiThieu;
                existingPromotion.SoNgayDatTruoc = model.SoNgayDatTruoc;
                existingPromotion.ChiApDungChoKhachMoi = model.ChiApDungChoKhachMoi;
                existingPromotion.ApDungChoTatCaPhong = true;
                existingPromotion.TrangThai = model.TrangThai;
                existingPromotion.SoLuong = model.SoLuong;
                existingPromotion.KhuyenMaiPhongs = null; // Không áp dụng phòng cụ thể

                await _khuyenMaiRepo.UpdateAsync(existingPromotion);
                _logger.LogInformation("Update: Successfully updated promotion with ID {Ma_KM}", model.Ma_KM);
                return Json(new { success = true, message = "Cập nhật khuyến mãi thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Update");
                return Json(new { success = false, message = ex.Message });
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
                if (promotion.ApDungKMs.Count() > 0 && DateTime.Now <= promotion.HSD)
                {
                    return Json(new { success = false, message = "Khuyến mãi này đã được áp dụng cho khách hàng và không thể xóa" });
                }
                await _khuyenMaiRepo.DeleteAsync(id);
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
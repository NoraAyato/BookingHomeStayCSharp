using DoAnCs.Models;
using DoAnCs.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DoAnCs.Areas.Admin.ModelsView;

namespace DoAnCs.Areas.Admin.Controllers
{
    [Route("Admin/Surcharge")]
    public class SurchargeController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SurchargeController> _logger;
        private readonly IPhuThuRepository _phuThuRepo;

        public SurchargeController(
            ApplicationDbContext context,
            ILogger<SurchargeController> logger,
            IPhuThuRepository phuThuRepo)
        {
            _context = context;
            _logger = logger;
            _phuThuRepo = phuThuRepo;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("GetSurcharges")]
        public async Task<JsonResult> GetSurcharges(string searchString, string dateRange, int page = 1, int pageSize = 10)
        {
            try
            {
                var query = _phuThuRepo.GetAllAsync();

                // Lọc theo searchString
                if (!string.IsNullOrEmpty(searchString))
                {
                    // Chuẩn hóa chuỗi tìm kiếm thành chữ thường
                    var searchLower = searchString.ToLower();
                    query = query.Where(s => s.NoiDung.ToLower().Contains(searchLower) ||
                                             s.Ma_PhieuPT.ToLower().Contains(searchLower));
                }

                // Lọc theo dateRange
                if (!string.IsNullOrEmpty(dateRange))
                {
                    var dates = dateRange.Split(" to ");
                    if (dates.Length == 2 && DateTime.TryParse(dates[0], out var startDate) && DateTime.TryParse(dates[1], out var endDate))
                    {
                        query = query.Where(s => s.NgayPhuThu >= startDate && s.NgayPhuThu <= endDate);
                    }
                }

                // Phân trang
                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
                var surcharges = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var surchargeList = surcharges.Select(s => new
                {
                    s.Ma_PhieuPT,
                    NgayPhuThu = s.NgayPhuThu.ToString("dd/MM/yyyy"),
                    s.NoiDung,
                    PhiPhuThu = s.PhiPhuThu * 100, // Chuyển thành phần trăm
                    AppliedRoomTypes = s.ApDungPhuThus.Select(ap => new
                    {
                        ap.ID_Loai,
                        ap.LoaiPhong.TenLoai,
                        NgayApDung = ap.NgayApDung.ToString("dd/MM/yyyy")
                    }).ToList()
                }).ToList();

                return Json(new
                {
                    success = true,
                    data = surchargeList,
                    totalPages,
                    currentPage = page,
                    totalItems
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetSurcharges");
                return Json(new { success = false, message = "Không thể tải danh sách phụ thu: " + ex.Message });
            }
        }

        [HttpGet("GetSurcharge/{id}")]
        public async Task<JsonResult> GetSurcharge(string id)
        {
            try
            {
                var surcharge = await _phuThuRepo.GetByIdAsync(id);
                if (surcharge == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy phiếu phụ thu" });
                }

                var result = new
                {
                    surcharge.Ma_PhieuPT,
                    NgayPhuThu = surcharge.NgayPhuThu.ToString("yyyy-MM-dd"),
                    surcharge.NoiDung,
                    PhiPhuThu = surcharge.PhiPhuThu * 100, // Chuyển thành phần trăm
                    AppliedRoomTypes = surcharge.ApDungPhuThus.Select(ap => ap.ID_Loai).ToList()
                };

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetSurcharge for {MaPhieuPT}", id);
                return Json(new { success = false, message = "Không thể tải chi tiết phụ thu: " + ex.Message });
            }
        }

        [HttpGet("GetRoomTypes")]
        public async Task<JsonResult> GetRoomTypes()
        {
            try
            {
                var roomTypes = await _context.LoaiPhongs
                    .Select(lp => new
                    {
                        lp.ID_Loai,
                        lp.TenLoai
                    })
                    .ToListAsync();

                return Json(new { success = true, data = roomTypes });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetRoomTypes");
                return Json(new { success = false, message = "Không thể tải danh sách loại phòng: " + ex.Message });
            }
        }

        [HttpPost("CreateSurcharge")]
        public async Task<JsonResult> CreateSurcharge([FromBody] SurchargeModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return Json(new { success = false, message = "Dữ liệu không hợp lệ", errors });
            }

            if (model.PhiPhuThu < 0 || model.PhiPhuThu > 100)
                return Json(new { success = false, message = "Phí phụ thu phải từ 0% đến 100%" });

            if (model.RoomTypeIds == null || !model.RoomTypeIds.Any())
                return Json(new { success = false, message = "Phải chọn ít nhất một loại phòng" });
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
               

                // Sinh Ma_PhieuPT duy nhất
                var maPhieuPT = "PT-" + DateTime.Now.ToString("yyyyMMddHHmmss");

                // Kiểm tra xem Ma_PhieuPT đã tồn tại chưa
                if (await _context.PhieuPhuThus.AnyAsync(p => p.Ma_PhieuPT == maPhieuPT))
                {
                    return Json(new { success = false, message = "Mã phiếu phụ thu đã tồn tại, vui lòng thử lại" });
                }

                // Tạo PhieuPhuThu mới
                var phieuPhuThu = new PhieuPhuThu
                {
                    Ma_PhieuPT = maPhieuPT,
                    NgayPhuThu = DateTime.Parse(model.NgayPhuThu),
                    NoiDung = model.NoiDung,
                    PhiPhuThu = model.PhiPhuThu / 100, // Chuyển từ phần trăm sang decimal
                    ApDungPhuThus = new List<ApDungPhuThu>()
                };

                // Tạo danh sách ApDungPhuThu mới
                foreach (var idLoai in model.RoomTypeIds)
                {
                    var apDungPhuThu = new ApDungPhuThu
                    {
                        ID_Loai = idLoai,
                        Ma_PhieuPT = maPhieuPT,
                        NgayApDung = DateTime.Parse(model.NgayPhuThu)
                    };

                    // Đảm bảo không có thực thể nào với cùng khóa chính đang được theo dõi
                    var existingEntry = _context.Entry(apDungPhuThu);
                    if (existingEntry.State != EntityState.Detached)
                    {
                        _context.Entry(apDungPhuThu).State = EntityState.Detached;
                    }

                    phieuPhuThu.ApDungPhuThus.Add(apDungPhuThu);
                }

                // Thêm PhieuPhuThu vào DbContext và lưu
                await _context.PhieuPhuThus.AddAsync(phieuPhuThu);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return Json(new { success = true, message = "Tạo phiếu phụ thu thành công" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error in CreateSurcharge");
                return Json(new { success = false, message = "Không thể tạo phiếu phụ thu: " + ex.Message });
            }
        }

        [HttpPost("UpdateSurcharge")]
        public async Task<JsonResult> UpdateSurcharge([FromBody] SurchargeModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return Json(new { success = false, message = "Dữ liệu không hợp lệ", errors });
            }

            if (model.PhiPhuThu < 0 || model.PhiPhuThu > 100)
                return Json(new { success = false, message = "Phí phụ thu phải từ 0% đến 100%" });

            if (model.RoomTypeIds == null || !model.RoomTypeIds.Any())
                return Json(new { success = false, message = "Phải chọn ít nhất một loại phòng" });
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                

                var phieuPhuThu = new PhieuPhuThu
                {
                    Ma_PhieuPT = model.Ma_PhieuPT,
                    NgayPhuThu = DateTime.Parse(model.NgayPhuThu),
                    NoiDung = model.NoiDung,
                    PhiPhuThu = model.PhiPhuThu / 100, // Chuyển từ phần trăm sang decimal
                    ApDungPhuThus = model.RoomTypeIds.Select(id => new ApDungPhuThu
                    {
                        ID_Loai = id,
                        Ma_PhieuPT = model.Ma_PhieuPT,
                        NgayApDung = DateTime.Parse(model.NgayPhuThu)
                    }).ToList()
                };

                await _phuThuRepo.UpdateAsync(phieuPhuThu);
                await transaction.CommitAsync();

                return Json(new { success = true, message = "Cập nhật phiếu phụ thu thành công" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error in UpdateSurcharge for {MaPhieuPT}", model.Ma_PhieuPT);
                return Json(new { success = false, message = "Không thể cập nhật phiếu phụ thu: " + ex.Message });
            }
        }

        [HttpPost("DeleteSurcharge/{id}")]
        public async Task<JsonResult> DeleteSurcharge(string id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                

                await _phuThuRepo.DeleteAsync(id);
                await transaction.CommitAsync();

                return Json(new { success = true, message = "Xóa phiếu phụ thu thành công" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error in DeleteSurcharge for {MaPhieuPT}", id);
                return Json(new { success = false, message = "Không thể xóa phiếu phụ thu: " + ex.Message });
            }
        }
    }
}
using DoAnCs.Models;
using DoAnCs.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace DoAnCs.Areas.Host.Controllers
{
    [Area("Host")]
    [Route("Host/Homestay")]
    [Authorize(Roles = "Host")] // Chỉ host được truy cập
    public class HomestayController : Controller
    {
        private readonly IHomestayRepository _homestayRepo;
        private readonly IChinhSachRepository _chinhSachRepo;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomestayController(
            IHomestayRepository homestayRepo,
            IChinhSachRepository chinhSachRepo,
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _homestayRepo = homestayRepo;
            _chinhSachRepo = chinhSachRepo;
            _context = context;
            _userManager = userManager;
        }

        // Hiển thị giao diện danh sách homestay của host
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // Lấy danh sách homestay của host (phân trang, lọc)
        [HttpGet("GetHomestaysFiltered")]
        public async Task<IActionResult> GetHomestaysFiltered(
            int page = 1,
            int pageSize = 6,
            string searchString = null,
            string locationFilter = null,
            string statusFilter = null,
            string sortOrder = null)
        {
            try
            {
                var userId = _userManager.GetUserId(User); // Lấy ID của host
                var homestays = await _homestayRepo.GetPaginatedByOwnerAsync(
                    userId, page, pageSize, searchString, locationFilter, statusFilter, sortOrder);
                var totalCount = await _homestayRepo.CountByOwnerAsync(
                    userId, searchString, locationFilter, statusFilter);

                var khuVucList = await _context.KhuVucs
                    .Select(k => new SelectListItem { Value = k.Ma_KV, Text = k.Ten_KV })
                    .ToListAsync();

                var homestayData = homestays.Select(h => new
                {
                    idHomestay = h.ID_Homestay,
                    tenHomestay = h.Ten_Homestay,
                    maKV = h.Ma_KV,
                    diaChi = h.DiaChi,
                    pricePerNight = h.PricePerNight,
                    trangThai = h.TrangThai,
                    hinhAnh = h.HinhAnh,
                    hang = h.Hang,
                    phongsCount = h.Phongs?.Count ?? 0,
                    khuVuc = h.KhuVuc != null ? new { tenKV = h.KhuVuc.Ten_KV } : null
                });

                var result = new
                {
                    success = true,
                    data = new
                    {
                        homestays = homestayData,
                        khuVucList = khuVucList.Select(k => new { value = k.Value, text = k.Text }),
                        totalCount = totalCount,
                        currentPage = page,
                        pageSize = pageSize,
                        totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                    }
                };

                return Json(result, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message },
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            }
        }

        // Lấy danh sách khu vực để populate dropdown
        [HttpGet("GetKhuVucList")]
        public async Task<IActionResult> GetKhuVucList()
        {
            try
            {
                var khuVucList = await _context.KhuVucs
                    .Select(k => new { value = k.Ma_KV, text = k.Ten_KV })
                    .ToListAsync();

                return Json(new { success = true, data = khuVucList },
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message },
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            }
        }

        // Lấy thông tin chi tiết homestay
        [HttpGet("GetHomestay")]
        public async Task<IActionResult> GetHomestay(string id)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var homestay = await _homestayRepo.GetByIdWithDetailsAsync(id);
                if (homestay == null || homestay.Ma_ND != userId)
                {
                    return Json(new { success = false, message = "Không tìm thấy homestay hoặc bạn không có quyền" });
                }

                var chinhSach = await _chinhSachRepo.GetByHomestayIdAsync(id);

                var result = new
                {
                    idHomestay = homestay.ID_Homestay,
                    tenHomestay = homestay.Ten_Homestay,
                    maKV = homestay.Ma_KV,
                    diaChi = homestay.DiaChi,
                    pricePerNight = homestay.PricePerNight,
                    trangThai = homestay.TrangThai,
                    hinhAnh = homestay.HinhAnh,
                    hang = homestay.Hang,
                    chinhSach = chinhSach != null ? new
                    {
                        maCS = chinhSach.Ma_CS,
                        nhanPhong = chinhSach.NhanPhong,
                        traPhong = chinhSach.TraPhong,
                        huyPhong = chinhSach.HuyPhong,
                        buaAn = chinhSach.BuaAn
                    } : null
                };

                return Json(new { success = true, data = result },
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message },
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            }
        }

        // Hiển thị giao diện quản lý chính sách của homestay
        [HttpGet("Policy/{homestayId}")]
        public async Task<IActionResult> Policy(string homestayId)
        {
            var userId = _userManager.GetUserId(User);
            var homestay = await _homestayRepo.GetByIdAsync(homestayId);
            if (homestay == null || homestay.Ma_ND != userId)
            {
                return NotFound("Homestay not found or you don't have permission.");
            }

            var chinhSach = await _chinhSachRepo.GetByHomestayIdAsync(homestayId);

            if (chinhSach == null)
            {
                chinhSach = new ChinhSach
                {
                    Ma_CS = "CS-" + Guid.NewGuid().ToString(),
                    ID_Homestay = homestayId,
                    NhanPhong = "14:00",
                    TraPhong = "12:00",
                    HuyPhong = "Hủy trước 48 giờ: hoàn tiền 100%. Sau đó: không hoàn tiền.",
                    BuaAn = "Không bao gồm bữa ăn."
                };
                await _chinhSachRepo.AddAsync(chinhSach);
            }

            ViewBag.HomestayName = homestay.Ten_Homestay;
            return View(chinhSach);
        }

        // Cập nhật hoặc tạo chính sách homestay
        [HttpPost("Policy/{homestayId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Policy(string homestayId, [FromBody] ChinhSach model)
        {
            if (string.IsNullOrEmpty(homestayId) || model == null)
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
            }

            try
            {
                var userId = _userManager.GetUserId(User);
                var homestay = await _homestayRepo.GetByIdAsync(homestayId);
                if (homestay == null || homestay.Ma_ND != userId)
                {
                    return Json(new { success = false, message = "Không tìm thấy homestay hoặc bạn không có quyền." });
                }

                var chinhSach = await _chinhSachRepo.GetByHomestayIdAsync(homestayId);

                if (chinhSach == null)
                {
                    chinhSach = new ChinhSach
                    {
                        Ma_CS = "CS-" + Guid.NewGuid().ToString(),
                        ID_Homestay = homestayId,
                        NhanPhong = model.NhanPhong,
                        TraPhong = model.TraPhong,
                        HuyPhong = model.HuyPhong,
                        BuaAn = model.BuaAn
                    };
                    await _chinhSachRepo.AddAsync(chinhSach);
                }
                else
                {
                    chinhSach.NhanPhong = model.NhanPhong;
                    chinhSach.TraPhong = model.TraPhong;
                    chinhSach.HuyPhong = model.HuyPhong;
                    chinhSach.BuaAn = model.BuaAn;
                    await _chinhSachRepo.UpdateAsync(chinhSach);
                }

                return Json(new { success = true, message = "Cập nhật chính sách thành công." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
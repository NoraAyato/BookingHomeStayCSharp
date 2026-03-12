using DoAnCs.Models;
using DoAnCs.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using System.Globalization;

namespace DoAnCs.Areas.Host.Controllers
{
    [Area("Host")]
    [Route("Host/Reviews")]
    [Authorize(Roles = "Host")]
    public class ReviewsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReviewsController> _logger;
        private readonly IDanhGiaRepository _danhGiaRepo;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReviewsController(
            ApplicationDbContext context,
            ILogger<ReviewsController> logger,
            IDanhGiaRepository danhGiaRepo,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _logger = logger;
            _danhGiaRepo = danhGiaRepo;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("GetReviews")]
        public async Task<JsonResult> GetReviews(
            string searchString,
            string dateRange,
            short? minRating,
            short? maxRating,
            int page = 1,
            int pageSize = 10)
        {
            try
            {
                var userId = _userManager.GetUserId(User);

                // Xử lý khoảng thời gian
                DateTime? startDate = null;
                DateTime? endDate = null;
                if (!string.IsNullOrEmpty(dateRange))
                {
                    var dates = dateRange.Split(" to ");
                    if (dates.Length == 2 && DateTime.TryParse(dates[0], out var start) && DateTime.TryParse(dates[1], out var end))
                    {
                        startDate = start;
                        endDate = end;
                    }
                }

                var homestayIds = await _context.Homestays
                    .Where(h => h.Ma_ND == userId)
                    .Select(h => h.ID_Homestay)
                    .ToListAsync();

                var reviews = await _danhGiaRepo.SearchAsync(searchString, homestayIds, startDate, endDate, minRating, maxRating);

                var totalItems = reviews.Count();
                var fiveStarCount = reviews.Count(r => r.Rating == 5);
                var lowStarCount = reviews.Count(r => r.Rating <= 2);
                var avgRating = totalItems > 0 ? reviews.Average(r => r.Rating) : 0;
                var lastMonthStart = DateTime.Now.AddMonths(-1).Date;
                var lastMonthEnd = DateTime.Now.Date;
                var lastMonthTotal = await _context.DanhGias
                    .Where(d => homestayIds.Contains(d.ID_Homestay) && d.NgayDanhGia >= lastMonthStart && d.NgayDanhGia <= lastMonthEnd)
                    .CountAsync();

                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
                var pagedReviews = reviews
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(r => new
                    {
                        r.ID_DG,
                        r.Ma_ND,
                        r.ID_Homestay,
                        r.Ma_PDPhong,
                        r.BinhLuan,
                        NgayDanhGia = r.NgayDanhGia.ToString("dd/MM/yyyy"),
                        r.HinhAnh,
                        r.Rating,
                        NguoiDung = r.NguoiDung?.UserName ?? "Không xác định",
                        UserAvatar = r.NguoiDung?.ProfilePicture ?? null,
                        UserEmail = r.NguoiDung?.Email ?? null,
                        Homestay = r.Homestay?.Ten_Homestay ?? "Không xác định",
                        PhieuDatPhong = r.PhieuDatPhong?.Ma_PDPhong ?? "Không xác định"
                    })
                    .ToList();

                return Json(new
                {
                    success = true,
                    data = pagedReviews,
                    totalPages,
                    currentPage = page,
                    totalItems,
                    stats = new
                    {
                        fiveStarCount,
                        lowStarCount,
                        avgRating = avgRating.ToString("F1"),
                        lastMonthTotal
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReviews for Host");
                return Json(new { success = false, message = "Không thể tải danh sách đánh giá: " + ex.Message });
            }
        }

        [HttpGet("GetReview/{idDG}")]
        public async Task<JsonResult> GetReview(string idDG)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var review = await _danhGiaRepo.GetByIdAsync(idDG);

                if (review == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đánh giá" });
                }

                var isHostHomestay = await _context.Homestays
                    .AnyAsync(h => h.ID_Homestay == review.ID_Homestay && h.Ma_ND == userId);
                if (!isHostHomestay)
                {
                    return Json(new { success = false, message = "Bạn không có quyền xem đánh giá này" });
                }

                var result = new
                {
                    review.ID_DG,
                    review.Ma_ND,
                    review.ID_Homestay,
                    review.Ma_PDPhong,
                    review.BinhLuan,
                    NgayDanhGia = review.NgayDanhGia.ToString("dd/MM/yyyy"),
                    review.HinhAnh,
                    review.Rating,
                    NguoiDung = review.NguoiDung?.UserName ?? "Không xác định",
                    UserAvatar = review.NguoiDung?.ProfilePicture ?? null,
                    UserEmail = review.NguoiDung?.Email ?? null,
                    Homestay = review.Homestay?.Ten_Homestay ?? "Không xác định",
                    PhieuDatPhong = review.PhieuDatPhong?.Ma_PDPhong ?? "Không xác định"
                };

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReview for ID_DG: {ID_DG}", idDG);
                return Json(new { success = false, message = "Không thể tải chi tiết đánh giá: " + ex.Message });
            }
        }

        [HttpPost("DeleteReview/{idDG}")]
        [Authorize(Roles = "Host")]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> DeleteReview(string idDG)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var review = await _danhGiaRepo.GetByIdAsync(idDG);

                if (review == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đánh giá" });
                }

                var isHostHomestay = await _context.Homestays
                    .AnyAsync(h => h.ID_Homestay == review.ID_Homestay && h.Ma_ND == userId);
                if (!isHostHomestay)
                {
                    return Json(new { success = false, message = "Bạn không có quyền xóa đánh giá này" });
                }

                await _danhGiaRepo.DeleteAsync(idDG);
                return Json(new { success = true, message = "Xóa đánh giá thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteReview for ID_DG: {ID_DG}", idDG);
                return Json(new { success = false, message = "Không thể xóa đánh giá: " + ex.Message });
            }
        }
    }
}
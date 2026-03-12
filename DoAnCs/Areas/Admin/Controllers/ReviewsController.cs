using DoAnCs.Models;
using DoAnCs.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace DoAnCs.Areas.Admin.Controllers
{
    [Route("Admin/Reviews")]
    public class ReviewsController : BaseController
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
            string idHomestay,
            string dateRange,
            short? minRating,
            short? maxRating,
            int page = 1,
            int pageSize = 10)
        {
            try
            {
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

                // Lấy danh sách đánh giá với bộ lọc
                var reviews = await _danhGiaRepo.SearchAsync(searchString, idHomestay, startDate, endDate, minRating, maxRating);

                // Phân trang
                var totalItems = reviews.Count();
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
                    totalItems
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReviews");
                return Json(new { success = false, message = "Không thể tải danh sách đánh giá: " + ex.Message });
            }
        }

        [HttpGet("GetReview/{idDG}")]
        public async Task<JsonResult> GetReview(string idDG)
        {
            try
            {
                var review = await _danhGiaRepo.GetByIdAsync(idDG);
                if (review == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đánh giá" });
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

        [HttpGet("GetHomestays")]
        public async Task<JsonResult> GetHomestays()
        {
            try
            {
                var homestays = await _context.Homestays
                    .Select(h => new
                    {
                        h.ID_Homestay,
                        h.Ten_Homestay
                    })
                    .ToListAsync();

                return Json(new { success = true, data = homestays });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetHomestays");
                return Json(new { success = false, message = "Không thể tải danh sách homestay: " + ex.Message });
            }
        }

        [HttpGet("GetUsers")]
        public async Task<JsonResult> GetUsers()
        {
            try
            {
                var users = await _userManager.Users
                    .Select(u => new
                    {
                        u.Id,
                        u.UserName
                    })
                    .ToListAsync();

                return Json(new { success = true, data = users });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetUsers");
                return Json(new { success = false, message = "Không thể tải danh sách người dùng: " + ex.Message });
            }
        }

        [HttpPost("DeleteReview/{idDG}")]
        public async Task<JsonResult> DeleteReview(string idDG)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var review = await _danhGiaRepo.GetByIdAsync(idDG);
                if (review == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đánh giá để xóa" });
                }

                await _danhGiaRepo.DeleteAsync(idDG);
                await transaction.CommitAsync();

                return Json(new { success = true, message = "Xóa đánh giá thành công" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error in DeleteReview for ID_DG: {ID_DG}", idDG);
                return Json(new { success = false, message = "Không thể xóa đánh giá: " + ex.Message });
            }
        }
    }
}
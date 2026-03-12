using DoAnCs.Models;
using DoAnCs.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;

namespace DoAnCs.Areas.Host.Controllers
{
    [Area("Host")]
    [Route("Host/Contracts")]
    [Authorize(Roles = "Host")]
    public class ContractController : Controller
    {
        private readonly IHopDongRepository _hopDongRepo;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ContractController> _logger;

        public ContractController(
            IHopDongRepository hopDongRepo,
            UserManager<ApplicationUser> userManager,
            ILogger<ContractController> logger)
        {
            _hopDongRepo = hopDongRepo ?? throw new ArgumentNullException(nameof(hopDongRepo));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("GetContracts")]
        public async Task<JsonResult> GetContracts(
        string searchQuery = "",
        string statusFilter = "all",
        string dateRange = "",
        int page = 1,
        int pageSize = 1) 
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                _logger.LogInformation("Fetching contracts for Host: {UserId}, Search: {SearchQuery}, Status: {StatusFilter}, DateRange: {DateRange}, Page: {Page}", userId, searchQuery, statusFilter, dateRange, page);

                // Sử dụng phương thức mới SearchForHostAsync
                var (hopDongs, totalRecords) = await _hopDongRepo.SearchForHostAsync(userId, searchQuery, statusFilter, dateRange, page, pageSize);

                // Chuyển đổi dữ liệu thành định dạng cần thiết
                var userContracts = hopDongs
                    .Select(h => new
                    {
                        h.Ma_HopDong,
                        h.Ten_Homestay,
                        h.DiaChi,
                        h.PricePerNight,
                        h.MoTa,
                        h.Hang,
                        h.HinhAnh,
                        h.TrangThai,
                        NgayGui = h.NgayGui.ToString("dd/MM/yyyy"),
                        NgayDuyet = h.NgayDuyet?.ToString("dd/MM/yyyy"),
                        h.LyDoTuChoi,
                        KhuVuc = h.KhuVuc?.Ten_KV ?? "Không xác định",
                        UserName = h.ApplicationUser?.UserName ?? "Không xác định"
                    })
                    .ToList();

                // Lấy thống kê trạng thái
                var stats = await _hopDongRepo.GetStatusStatisticsForHostAsync(userId,searchQuery, dateRange);

                var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

                return Json(new
                {
                    success = true,
                    data = userContracts,
                    totalPages,
                    currentPage = page,
                    totalItems = totalRecords,
                    stats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetContracts for Host");
                return Json(new { success = false, message = "Không thể tải danh sách hợp đồng: " + ex.Message });
            }
        }

        [HttpGet("GetContract/{maHopDong}")]
        public async Task<JsonResult> GetContract(string maHopDong)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var contract = await _hopDongRepo.GetByIdAsync(maHopDong);

                if (contract == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy hợp đồng" });
                }

                if (contract.Ma_ND != userId)
                {
                    return Json(new { success = false, message = "Bạn không có quyền xem hợp đồng này" });
                }

                var result = new
                {
                    contract.Ma_HopDong,
                    contract.Ten_Homestay,
                    contract.DiaChi,
                    contract.PricePerNight,
                    contract.MoTa,
                    contract.Hang,
                    contract.HinhAnh,
                    contract.TrangThai,
                    NgayGui = contract.NgayGui.ToString("dd/MM/yyyy"),
                    NgayDuyet = contract.NgayDuyet?.ToString("dd/MM/yyyy"),
                    contract.LyDoTuChoi,
                    KhuVuc = contract.KhuVuc?.Ten_KV ?? "Không xác định",
                    UserName = contract.ApplicationUser?.UserName ?? "Không xác định"
                };

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetContract for Ma_HopDong: {Ma_HopDong}", maHopDong);
                return Json(new { success = false, message = "Không thể tải chi tiết hợp đồng: " + ex.Message });
            }
        }

        [HttpPost("CancelContract/{maHopDong}")]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> CancelContract(string maHopDong, [FromBody] string lyDoHuy)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var contract = await _hopDongRepo.GetByIdAsync(maHopDong);

                if (contract == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy hợp đồng" });
                }

                if (contract.Ma_ND != userId)
                {
                    return Json(new { success = false, message = "Bạn không có quyền hủy hợp đồng này" });
                }

                if (contract.TrangThai == "Đã hủy")
                {
                    return Json(new { success = false, message = "Hợp đồng đã được hủy trước đó" });
                }

                if (string.IsNullOrEmpty(lyDoHuy))
                {
                    return Json(new { success = false, message = "Lý do hủy không được để trống" });
                }

                var result = await _hopDongRepo.CreateCancellationAsync(maHopDong, userId, lyDoHuy);

                if (result)
                {
                    return Json(new { success = true, message = "Hủy hợp đồng thành công" });
                }

                else
                {
                    return Json(new { success = false, message = "Không thể hủy hợp đồng" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CancelContract for Ma_HopDong: {Ma_HopDong}", maHopDong);
                return Json(new { success = false, message = "Không thể hủy hợp đồng: " + ex.Message });
            }
        }
    }
}
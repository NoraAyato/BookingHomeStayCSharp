using DoAnCs.Areas.Admin.ModelsView;
using DoAnCs.Models;
using DoAnCs.Repository;
using DoAnCs.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DoAnCs.Areas.Admin.Controllers
{
  
    [Route("Admin/Contract")]
    public class ContractController : BaseController
    {
        private readonly IHopDongRepository _hopDongRepo;
        private readonly IHomestayRepository _homestayRepo;
        private readonly IChinhSachRepository _chinhSachRepo;
        private readonly IEmailService _emailService;
        private readonly IUserRepository _userRepo;
        private readonly UserManager<ApplicationUser> _userManager;
        public ContractController(
            IHopDongRepository hopDongRepo,
            IHomestayRepository homestayRepo,
            IChinhSachRepository chinhSachRepo,
            IEmailService emailService,
            IUserRepository userRepo,
            UserManager<ApplicationUser> userManager)
        {
            _hopDongRepo = hopDongRepo;
            _homestayRepo = homestayRepo;
            _chinhSachRepo = chinhSachRepo;
            _emailService = emailService;
            _userRepo = userRepo;
            _userManager = userManager;
        }

        [HttpGet("Index")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("GetHopDongs")]
        public async Task<IActionResult> GetHopDongs(string searchQuery = "", string statusFilter = "all", string dateRange = "", int pageNumber = 1, int pageSize = 10)
        {
            var (hopDongs, totalRecords) = await _hopDongRepo.SearchAsync(searchQuery, statusFilter, dateRange, pageNumber, pageSize);
            var stats = await _hopDongRepo.GetStatusStatisticsAsync(searchQuery, dateRange);

            var hopDongList = hopDongs.Select(hd => new
            {
                hd.Ma_HopDong,
                hd.Ten_Homestay,
                hd.DiaChi,
                hd.PricePerNight,
                hd.Hang,
                hd.HinhAnh,
                hd.NgayGui,
                hd.NgayDuyet,
                hd.TrangThai,
                hd.Ma_KV,
                hd.LyDoTuChoi
            }).ToList();

            return Json(new
            {
                success = true,
                data = hopDongList,
                totalRecords,
                totalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
                currentPage = pageNumber,
                stats
            });
        }

        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(string id)
        {
            var hopDong = await _hopDongRepo.GetByIdAsync(id);
            if (hopDong == null)
            {
                return Json(new { success = false, message = "Không tìm thấy hợp đồng" });
            }

            var hopDongData = new
            {
                hopDong.Ma_HopDong,
                hopDong.Ten_Homestay,
                hopDong.DiaChi,
                hopDong.PricePerNight,
                hopDong.Hang,
                hopDong.HinhAnh,
                hopDong.MoTa,
                hopDong.NgayGui,
                hopDong.NgayDuyet,
                hopDong.TrangThai,
                hopDong.Ma_KV,
                hopDong.LyDoTuChoi
            };

            return Json(new { success = true, data = hopDongData });
        }

        [HttpPost("UpdateStatus/{id}")]
        public async Task<IActionResult> UpdateStatus(string id, [FromForm] UpdateHopDongStatusModel model)
        {
            try
            {
                // Kiểm tra đầu vào
                var validationResult = ValidateInput(model);
                if (validationResult != null)
                {
                    return Json(validationResult);
                }

                // Tìm hợp đồng
                var hopDong = await GetHopDong(id);
                if (hopDong == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy hợp đồng" });
                }

                // Xử lý trạng thái
                if (model.TrangThai == "Đã duyệt")
                {
                    var approvalResult = await HandleApprovedStatus(hopDong);
                    if (!approvalResult.Success)
                    {
                        return Json(new { success = false, message = approvalResult.Message, errors = approvalResult.Errors });
                    }
                }
                else if (model.TrangThai == "Từ chối")
                {
                    var rejectionResult = await HandleRejectedStatus(hopDong, model);
                    if (!rejectionResult.Success)
                    {
                        return Json(new { success = false, message = rejectionResult.Message });
                    }
                }
                else
                {
                    hopDong.LyDoTuChoi = null;
                }

                // Cập nhật trạng thái hợp đồng
                var updateResult = await UpdateHopDongStatus(hopDong, model.TrangThai);
                return Json(updateResult);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra, vui lòng thử lại" });
            }
        }

        private object ValidateInput(UpdateHopDongStatusModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return new { success = false, message = "Dữ liệu không hợp lệ", errors };
            }
            return null;
        }

        private async Task<HopDong> GetHopDong(string id)
        {
            return await _hopDongRepo.GetByIdAsync(id);
        }

        private async Task<(bool Success, string Message, object Errors)> HandleApprovedStatus(HopDong hopDong)
        {
            hopDong.NgayDuyet = DateTime.Now;

            // Tạo homestay
            var home = new Homestay
            {
                ID_Homestay = "HS-" + Guid.NewGuid().ToString(),
                Ten_Homestay = hopDong.Ten_Homestay,
                DiaChi = hopDong.DiaChi,
                PricePerNight = hopDong.PricePerNight,
                Hang = hopDong.Hang,
                HinhAnh = hopDong.HinhAnh,
                Ma_KV = hopDong.Ma_KV,
                Ma_ND = hopDong.Ma_ND,
                TrangThai = "Hoạt động"
            };
            await _homestayRepo.AddAsync(home);

            // Tạo chính sách
            var chinhSach = new ChinhSach
            {
                Ma_CS = "CS-" + Guid.NewGuid().ToString(),
                ID_Homestay = home.ID_Homestay,
                NhanPhong = "14:00",
                TraPhong = "12:00",
                HuyPhong = "Hủy trước 48 giờ: hoàn tiền 100%. Sau đó: không hoàn tiền.",
                BuaAn = "Không bao gồm bữa ăn."
            };
            await _chinhSachRepo.AddAsync(chinhSach);

            // Cập nhật vai trò người dùng
            var user = await _userRepo.GetByIdAsync(hopDong.Ma_ND);
            if (user == null)
            {
                return (false, "Không tìm thấy người dùng", null);
            }

            var roleResult = await UpdateUserRoleToHost(user);
            if (!roleResult.Success)
            {
                return roleResult;
            }

            // Gửi email thông báo
            await SendApprovalEmail(user, hopDong, home);

            return (true, "Xử lý trạng thái duyệt thành công", null);
        }

        private async Task<(bool Success, string Message)> HandleRejectedStatus(HopDong hopDong, UpdateHopDongStatusModel model)
        {
            if (string.IsNullOrEmpty(model.LyDoTuChoi))
            {
                return (false, "Lý do từ chối không được để trống khi từ chối hợp đồng");
            }

            hopDong.LyDoTuChoi = model.LyDoTuChoi;

            // Gửi email thông báo từ chối
            var user = await _userRepo.GetByIdAsync(hopDong.Ma_ND);
            if (user != null && !string.IsNullOrEmpty(user.Email))
            {
                await SendRejectionEmail(user, hopDong);
            }

            return (true, "X= null");
        }

        private async Task<object> UpdateHopDongStatus(HopDong hopDong, string trangThai)
        {
            hopDong.TrangThai = trangThai;
            await _hopDongRepo.UpdateAsync(hopDong);

            return new
            {
                success = true,
                message = "Cập nhật trạng thái hợp đồng thành công",
                data = new
                {
                    hopDong.Ma_HopDong,
                    hopDong.Ten_Homestay,
                    hopDong.DiaChi,
                    hopDong.PricePerNight,
                    hopDong.Hang,
                    hopDong.HinhAnh,
                    hopDong.NgayGui,
                    hopDong.NgayDuyet,
                    hopDong.TrangThai,
                    hopDong.LyDoTuChoi
                }
            };
        }

        private async Task SendApprovalEmail(ApplicationUser user, HopDong hopDong, Homestay home)
        {
            if (!string.IsNullOrEmpty(user.Email))
            {
                var subject = "Hợp đồng của bạn đã được duyệt";
                var body = $@"
                    <h2>Xin chào {user.FullName},</h2>
                    <p>Hợp đồng của bạn với mã <strong>{hopDong.Ma_HopDong}</strong> đã được duyệt thành công.</p>
                    <p>Thông tin homestay của bạn đã được tạo với tên: <strong>{home.Ten_Homestay}</strong>.</p>
                    <p>Trạng thái: <strong>Hoạt động</strong></p>
                    <p>Ngày tạo: <strong>{DateTime.Now:dd/MM/yyyy}</strong></p>
                    <p>Cảm ơn bạn đã đăng ký!</p>
                    <p>Trân trọng,<br/>Đội ngũ Rose Homestay</p>
                ";
                await _emailService.SendEmailAsync(user.Email, subject, body);
            }
        }

        private async Task SendRejectionEmail(ApplicationUser user, HopDong hopDong)
        {
            if (!string.IsNullOrEmpty(user.Email))
            {
                var subject = "Hợp đồng của bạn đã bị từ chối";
                var body = $@"
                    <h2>Xin chào {user.FullName},</h2>
                    <p>Hợp đồng của bạn với mã <strong>{hopDong.Ma_HopDong}</strong> đã bị từ chối.</p>
                    <p>Lý do: <strong>{hopDong.LyDoTuChoi}</strong></p>
                    <p>Vui lòng chỉnh sửa và gửi lại hợp đồng nếu cần.</p>
                    <p>Trân trọng,<br/>Đội ngũ Rose Homestay</p>
                ";
                await _emailService.SendEmailAsync(user.Email, subject, body);
            }
        }

        private async Task<(bool Success, string Message, object Errors)> UpdateUserRoleToHost(ApplicationUser user)
        {
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    var removeErrors = removeResult.Errors.Select(e => e.Description).ToList();
                    return (false, "Lỗi trong quá trình cập nhật vai trò", removeErrors);
                }
            }

            var addResult = await _userManager.AddToRoleAsync(user, "Host");
            if (!addResult.Succeeded)
            {
                var addErrors = addResult.Errors.Select(e => e.Description).ToList();
                return (false, "Thêm vai trò Host thất bại", addErrors);
            }

            return (true, "Cập nhật vai trò thành công", null);
        }
    }
}
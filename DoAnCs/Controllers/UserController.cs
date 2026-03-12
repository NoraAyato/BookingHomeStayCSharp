using DoAnCs.Models;
using DoAnCs.Models.ViewModels;
using DoAnCs.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nest;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static DoAnCs.Models.ViewModels.MyBooking;

namespace DoAnCs.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPhieuDatPhongRepository _phieuDatPhongRepository;
        private readonly IKhuyenMaiRepository _khuyenMaiRepository;
        private readonly IKhuVucRepository _khuVucRepository;
        private readonly IHopDongRepository _hopDongRepository;
        private readonly IServiceRepository _serviceRepository;
        private readonly IHuyPhongRepository _huyPhongRepository;
        private readonly IHoaDonRepository _hoaDonRepository;
        private readonly IDanhGiaRepository _danhGiaRepository;
        private readonly IPhuThuRepository _phuThuRepo;

        
        public UserController(
            UserManager<ApplicationUser> userManager,
            IPhieuDatPhongRepository phieuDatPhongRepository,
            IKhuyenMaiRepository khuyenMaiRepository,
            IKhuVucRepository khuVucRepository,
            IHopDongRepository hopDongRepository,
            IServiceRepository serviceRepository,
            IHuyPhongRepository huyPhongRepository,
            IHoaDonRepository hoaDonRepository,
            IDanhGiaRepository danhGiaRepository,IPhuThuRepository phuThuRepository)
        {
            _userManager = userManager;
            _phieuDatPhongRepository = phieuDatPhongRepository;
            _khuyenMaiRepository = khuyenMaiRepository;
            _khuVucRepository = khuVucRepository;
            _hopDongRepository = hopDongRepository;
            _serviceRepository = serviceRepository;
            _huyPhongRepository = huyPhongRepository;
            _hoaDonRepository = hoaDonRepository;
            _danhGiaRepository = danhGiaRepository;
            _phuThuRepo = phuThuRepository;
        }

        // Xem và cập nhật hồ sơ người dùng
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            return View();
        }

        public async Task<IActionResult> GetProfile(int pageNumber = 1)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy người dùng." });
            }

           
            var model = new ProfileUserViewModel
            {
                Email = user.Email,
                FullName = user.FullName?.ToString(),
                Address = user.Address,
                DateOfBirth = user.DateOfBirth,
                ProfilePicture = user.ProfilePicture,
                TrangThai = user.TrangThai,
                TichDiem = user.tichdiem ?? 0,
                PhoneNumber = user.PhoneNumber,
                CurrentPage = pageNumber,
            };

            return Json(new { success = true, data = model });
        }
        // Cập nhật trạng thái nhận email thông báo
        [HttpPost]
        public async Task<IActionResult> UpdateNotificationPreference(string email,bool receiveEmailNotifications)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy người dùng." });
            }
            if (user.Email != email)
            {
                return Json(new { success = false, message = "Email không khớp với tài khoản." });
            }
            user.IsRecieveEmail = receiveEmailNotifications;
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                return Json(new { success = true, message = "Cập nhật thành công." });
            }
            else
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return Json(new { success = false, message = "Cập nhật thất bại.", errors });
            }
        }
        //kiểm tra xem người dùng có nhận email thông báo hay không
        [HttpGet]
        public async Task<IActionResult> GetNotificationPreference()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy người dùng." });
            }
            return Json(new { success = true, receiveEmailNotifications = user.IsRecieveEmail });
        }
        public async Task<IActionResult> GetMyBooking(int pageNumber = 1)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy người dùng." });
            }

            const int pageSize = 3;
            var (phieuDatPhongs, totalCount) = await _phieuDatPhongRepository.GetByUserIdWithPaginationAsync(user.Id, pageNumber, pageSize);

            var model = new MyBookingDTO
            {
                PhieuDatPhongs = phieuDatPhongs.Select(p => new PhieuDatPhongDTO
                {
                    MaPDPhong = p.Ma_PDPhong,
                    NgayLap = p.NgayLap,
                    TrangThai = p.TrangThai,
                    ChiTietDatPhongs = p.ChiTietDatPhongs.Select(c => new ChiTietDatPhongDTO
                    {
                        Phong = new PhongDTO
                        {
                            TenPhong = c.Phong.TenPhong,
                            Homestay = new HomestayDTO { TenHomestay = c.Phong.Homestay.Ten_Homestay },
                            HinhAnhPhongs = c.Phong.HinhAnhPhongs.Select(h => new HinhAnhPhongDTO
                            {
                                UrlAnh = h.UrlAnh,
                                LaAnhChinh = h.LaAnhChinh
                            }).ToList()
                        },
                        NgayDen = c.NgayDen,
                        NgayDi = c.NgayDi
                    }).ToList()
                }).ToList(),
                CurrentPage = pageNumber,
                TotalPages = (int)Math.Ceiling((double)phieuDatPhongs.Count() / pageSize)
            };

            return Json(new { success = true, data = model });
        }
        private decimal CalculateTotalPrice(PhieuDatPhong booking)
        {
            decimal total = 0;

            foreach (var detail in booking.ChiTietDatPhongs)
            {
                var nights = (detail.NgayDi - detail.NgayDen).Days;
                total += nights * (detail.Phong.DonGia + _phuThuRepo.CalculatePhuThuAsync(detail.Phong.ID_Loai, detail.NgayDen, detail.NgayDi, detail.Phong.DonGia).Result);
                var hoaDon = _hoaDonRepository.GetByPhieuDatPhongAsync(booking.Ma_PDPhong).Result;
                if (hoaDon != null)
                {
                    if (hoaDon.ApDungKMs != null && hoaDon.ApDungKMs.Any())
                    {
                        var khuyenMai = _khuyenMaiRepository.GetByIdAsync(hoaDon.ApDungKMs.First().Ma_KM).Result;
                        if (khuyenMai != null)
                        {
                            total -= total * (khuyenMai.ChietKhau / 100);
                        }
                    }
                }
                foreach (var _service in detail.PhieuSuDungDVs)
                {
                    total += _service.ChiTietPhieuDVs.Sum(ct => ct.SoLuong * ct.DichVu.DonGia);
                }
            }
            return total;
        }
        private decimal CalculateTotalPriceHaveToPay(PhieuDatPhong booking)
        {
            decimal total = 0;
            decimal dv = 0;
            foreach (var detail in booking.ChiTietDatPhongs)
            {
                var nights = (detail.NgayDi - detail.NgayDen).Days;
                total += nights * (detail.Phong.DonGia + _phuThuRepo.CalculatePhuThuAsync(detail.Phong.ID_Loai, detail.NgayDen, detail.NgayDi, detail.Phong.DonGia).Result);
                var hoaDon = _hoaDonRepository.GetByPhieuDatPhongAsync(booking.Ma_PDPhong).Result;
                if (hoaDon != null)
                {
                    if (hoaDon.ApDungKMs != null && hoaDon.ApDungKMs.Any())
                    {
                        var khuyenMai = _khuyenMaiRepository.GetByIdAsync(hoaDon.ApDungKMs.First().Ma_KM).Result;
                        if (khuyenMai != null)
                        {
                            total -= total * (khuyenMai.ChietKhau / 100);
                        }
                    }
                }
                foreach (var _service in detail.PhieuSuDungDVs)
                {
                    dv += _service.ChiTietPhieuDVs.Sum(ct => ct.SoLuong * ct.DichVu.DonGia);
                }
            }
            total *= 0.85m; // tiền khấu trừ 15% phí từ hoa hồng cho website
            total += dv; // cộng tiền dịch vụ
            return total;
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadProfileImage(IFormFile profileImage)
        {
            // Kiểm tra file đầu vào
            if (profileImage == null || profileImage.Length == 0)
            {
                return Json(new { success = false, message = "Vui lòng chọn một file ảnh." });
            }

            // Kiểm tra định dạng file
            var validTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/jpg" };
            if (!validTypes.Contains(profileImage.ContentType.ToLower()))
            {
                return Json(new { success = false, message = "Chỉ chấp nhận file ảnh (JPEG, PNG, GIF, JPG)." });
            }

            // Kiểm tra kích thước file (tối đa 5MB)
            if (profileImage.Length > 5 * 1024 * 1024)
            {
                return Json(new { success = false, message = "Kích thước file tối đa là 5MB." });
            }

            try
            {
                // Lấy thông tin người dùng
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng." });
                }

                // Tạo thư mục lưu trữ nếu chưa tồn tại
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/avatars");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Xóa ảnh đại diện cũ nếu có
                if (!string.IsNullOrEmpty(user.ProfilePicture))
                {
                    var oldImagePath = Path.Combine(uploadsFolder, Path.GetFileName(user.ProfilePicture.TrimStart('/')));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        try
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                        catch (IOException ex)
                        {
                            // Ghi log lỗi nhưng không làm gián đoạn quy trình
                            Console.WriteLine($"Lỗi khi xóa ảnh cũ: {ex.Message}");
                        }
                    }
                }

                // Tạo tên file duy nhất
                var fileExtension = Path.GetExtension(profileImage.FileName).ToLower();
                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                // Lưu file ảnh
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await profileImage.CopyToAsync(stream);
                }

                // Cập nhật đường dẫn ảnh trong model
                user.ProfilePicture = $"/uploads/avatars/{fileName}";

                // Cập nhật thông tin người dùng
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    var errors = updateResult.Errors.Select(e => e.Description).ToList();
                    return Json(new { success = false, message = "Cập nhật ảnh thất bại.", errors });
                }

                return Json(new
                {
                    success = true,
                    message = "Cập nhật ảnh đại diện thành công!",
                    profilePicture = user.ProfilePicture
                });
            }
            catch (Exception ex)
            {
                // Ghi log lỗi chi tiết để debug
                Console.WriteLine($"Lỗi khi tải lên ảnh: {ex.Message}");
                return Json(new { success = false, message = $"Lỗi server: {ex.Message}" });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(ProfileUserViewModel model)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng." });
                }

                user.FullName = model.FullName;
                user.PhoneNumber = model.PhoneNumber;
                user.DateOfBirth = model.DateOfBirth;
                user.Address = model.Address;

                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    var errors = updateResult.Errors.Select(e => e.Description).ToList();
                    return Json(new { success = false, errors = errors });
                }

                return Json(new { success = true, message = "Cập nhật thông tin thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi server: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfilePicture(IFormFile profileImage)
        {
            if (profileImage == null || profileImage.Length == 0)
            {
                return Json(new { success = false, message = "Vui lòng chọn một file ảnh." });
            }

            var validTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/jpg" };
            if (!validTypes.Contains(profileImage.ContentType))
            {
                return Json(new { success = false, message = "Chỉ chấp nhận file ảnh (JPEG, PNG, GIF)." });
            }

            if (profileImage.Length > 5 * 1024 * 1024)
            {
                return Json(new { success = false, message = "Kích thước file tối đa là 5MB." });
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng." });
                }

                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/users");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                if (!string.IsNullOrEmpty(user.ProfilePicture))
                {
                    var oldImagePath = Path.Combine(uploadsFolder, Path.GetFileName(user.ProfilePicture));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(profileImage.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await profileImage.CopyToAsync(stream);
                }

                user.ProfilePicture = $"/uploads/users/{fileName}";
                var updateResult = await _userManager.UpdateAsync(user);

                if (!updateResult.Succeeded)
                {
                    return Json(new { success = false, message = "Có lỗi xảy ra khi cập nhật hình ảnh." });
                }

                return Json(new { success = true, message = "Cập nhật hình ảnh thành công!", profilePicture = user.ProfilePicture });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi server: " + ex.Message });
            }
        }

        // Xem chi tiết đặt phòng
        [HttpGet]
        public async Task<IActionResult> GetBookingDetails(string maPDPhong)
        {
            var phieu = await _phieuDatPhongRepository.GetByIdAsync(maPDPhong);
            if (phieu == null)
            {
                return Json(new { success = false, message = "Không tìm thấy phiếu đặt phòng." });
            }

            var user = await _userManager.GetUserAsync(User);
            if (phieu.Ma_ND != user.Id)
            {
                return Json(new { success = false, message = "Bạn không có quyền xem phiếu này." });
            }
            var tongTien = phieu.TrangThai == "Đã xác nhận" ? CalculateTotalPriceHaveToPay(phieu) : CalculateTotalPrice(phieu);
            var chiTietPhongs = (phieu.ChiTietDatPhongs ?? Enumerable.Empty<ChiTietDatPhong>()).Select(ct => new
            {
                idHomestay = ct.Phong?.Homestay?.ID_Homestay ?? "Không xác định",
                tenPhong = ct.Phong?.TenPhong ?? "Không xác định",
                tenHomestay = ct.Phong?.Homestay?.Ten_Homestay ?? "Không xác định",
                ngayDen = ct.NgayDen.ToString("dd/MM/yyyy"),
                ngayDi = ct.NgayDi.ToString("dd/MM/yyyy"),
                hinhAnhChinh = ct.Phong?.HinhAnhPhongs?.FirstOrDefault(ha => ha.LaAnhChinh)?.UrlAnh ?? "/images/default-room.jpg",
                dichVus = (ct.PhieuSuDungDVs ?? Enumerable.Empty<PhieuSuDungDV>())
                    .SelectMany(ps => ps.ChiTietPhieuDVs ?? Enumerable.Empty<ChiTietPhieuDV>())
                    .Select(c => new
                    {
                        tenDV = c.DichVu?.Ten_DV ?? "Không xác định",
                        soLuong = c.SoLuong,
                        donGia = c.DichVu?.DonGia ?? 0
                    })
            }).ToList();

            return Json(new
            {
                success = true,
                data = new
                {
                    maPDPhong = phieu.Ma_PDPhong,
                    tongTien,
                    ngayLap = phieu.NgayLap.ToString("dd/MM/yyyy"),
                    trangThai = phieu.TrangThai,
                    chiTietPhongs
                }
            });
        }

        // Xem danh sách khuyến mãi khả dụng
        [HttpGet]
        public async Task<IActionResult> AvailablePromotions()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailablePromotions()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy người dùng." });
            }

            try
            {
                var now = DateTime.Now;
                var activePromotions = await _khuyenMaiRepository.GetAllQueryable()
                    .Where(km => km.TrangThai == "Đang áp dụng" &&
                                 km.NgayBatDau <= now &&
                                 km.HSD >= now &&
                                 km.SoLuong > 0)
                    .ToListAsync();

                var eligiblePromotions = new List<PromotionViewModel>();
                foreach (var km in activePromotions)
                {
                    bool isEligible = true;

                    if (km.ChiApDungChoKhachMoi)
                    {
                        var hasBooking = await _phieuDatPhongRepository.HasBookingAsync(user.Id);
                        if (hasBooking)
                        {
                            isEligible = false;
                        }
                    }

                    if (!isEligible) continue;

                    var conditions = new List<string>();
                    if (km.SoDemToiThieu.HasValue) conditions.Add($"Tối thiểu {km.SoDemToiThieu} đêm");
                    if (km.SoNgayDatTruoc.HasValue) conditions.Add($"Đặt trước {km.SoNgayDatTruoc} ngày");     
                    if (km.ChiApDungChoKhachMoi) conditions.Add("Chỉ khách mới");
                    if (km.ApDungChoTatCaPhong) conditions.Add("Tất cả homestay");
                    else if (km.KhuyenMaiPhongs != null && km.KhuyenMaiPhongs.Any())
                        conditions.Add($"Áp dụng cho {km.KhuyenMaiPhongs.First().Homestay.Ten_Homestay} : {km.KhuyenMaiPhongs.Count} phòng");
                  

                    var conditionText = conditions.Any() ? string.Join(", ", conditions) : null;

                    eligiblePromotions.Add(new PromotionViewModel
                    {
                        MaKM = km.Ma_KM,
                        NoiDung = km.NoiDung,
                        ChietKhau = km.ChietKhau,
                        LoaiChietKhau = km.LoaiChietKhau,
                        HSD = km.HSD.ToString("dd/MM/yyyy"),
                        DieuKien = conditionText,
                        SoLuongConLai = (int)km.SoLuong
                    });
                }

                return Json(new { success = true, promotions = eligiblePromotions });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi lấy danh sách khuyến mãi: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ApplyPromotion([FromBody] ApplyPromotionViewModel model)
        {
            if (string.IsNullOrEmpty(model.PromotionId))
            {
                return Json(new { success = false, message = "Mã khuyến mãi không được để trống." });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy người dùng." });
            }

            try
            {
                var promotion = await _khuyenMaiRepository.GetByIdAsync(model.PromotionId);
                if (promotion == null)
                {
                    return Json(new { success = false, message = "Khuyến mãi không tồn tại." });
                }

                var now = DateTime.Now;
                if (promotion.TrangThai != "Đang áp dụng" || promotion.NgayBatDau > now || promotion.HSD < now || promotion.SoLuong <= 0)
                {
                    return Json(new { success = false, message = "Khuyến mãi không hợp lệ hoặc đã hết số lượng." });
                }

                if (promotion.ChiApDungChoKhachMoi)
                {
                    var hasBooking = await _phieuDatPhongRepository.HasBookingAsync(user.Id);
                    if (hasBooking)
                    {
                        return Json(new { success = false, message = "Khuyến mãi chỉ áp dụng cho khách mới." });
                    }
                }

                HttpContext.Session.SetString("AppliedPromotion", model.PromotionId);
                return Json(new { success = true, message = $"Áp dụng khuyến mãi {promotion.Ma_KM} thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi áp dụng khuyến mãi: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> RegisterHomestay()
        {
            return View();
        }

        

        // Xử lý gửi đơn đăng ký hợp đồng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterHomestay(RegisterHomestayViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return Json(new { success = false, message = "Dữ liệu không hợp lệ.", errors });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy người dùng." });
            }

            try
            {
                // Kiểm tra Ma_KV có tồn tại không
                var khuVuc = await _khuVucRepository.GetByIdAsync(model.Ma_KV);
                if (khuVuc == null)
                {
                    return Json(new { success = false, message = "Khu vực không hợp lệ." });
                }

                decimal hangValue;
                if (!decimal.TryParse(Request.Form["Hang"], NumberStyles.Any, CultureInfo.InvariantCulture, out hangValue))
                {
                    return Json(new { success = false, message = "Hạng không hợp lệ." });
                }

                var hopDong = new HopDong
                {
                    Ma_HopDong = "CT-" + Guid.NewGuid().ToString(),
                    Ma_ND = user.Id,
                    Ten_Homestay = model.Ten_Homestay,
                    DiaChi = model.DiaChi,
                    PricePerNight = model.PricePerNight,
                    Ma_KV = model.Ma_KV,
                    MoTa = model.MoTa,
                    NgayGui = DateTime.Now,
                    TrangThai = "Đang chờ duyệt",
                    Hang = hangValue
                };

                // Xử lý upload hình ảnh
                if (model.HinhAnh != null && model.HinhAnh.Length > 0)
                {
                    var validTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/jpg" };
                    if (!validTypes.Contains(model.HinhAnh.ContentType))
                    {
                        return Json(new { success = false, message = "Chỉ chấp nhận file ảnh (JPEG, PNG, GIF)." });
                    }

                    if (model.HinhAnh.Length > 5 * 1024 * 1024)
                    {
                        return Json(new { success = false, message = "Kích thước file tối đa là 5MB." });
                    }

                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img/Homestays");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.HinhAnh.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.HinhAnh.CopyToAsync(stream);
                    }

                    hopDong.HinhAnh = $"/img/Homestays/{fileName}";
                }
                else
                {
                    return Json(new { success = false, message = "Hình ảnh là bắt buộc." });
                }

                await _hopDongRepository.AddAsync(hopDong);

                return Json(new { success = true, message = "Gửi đơn đăng ký thành công! Vui lòng chờ admin duyệt." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi lưu hợp đồng: " + ex.Message });
            }
        }
        [HttpGet]
        public async Task<IActionResult> CheckUserProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy người dùng." });
            }

            // Kiểm tra các trường bắt buộc
            var missingFields = new List<string>();
            if (string.IsNullOrWhiteSpace(user.FullName))
                missingFields.Add("Họ tên");
            if (string.IsNullOrWhiteSpace(user.ProfilePicture))
                missingFields.Add("Hình ảnh đại diện");
            if (string.IsNullOrWhiteSpace(user.PhoneNumber))
                missingFields.Add("Số điện thoại");
            if (string.IsNullOrWhiteSpace(user.Address))
                missingFields.Add("Địa chỉ");

            if (missingFields.Any())
            {
                var message = "Vui lòng cập nhật đầy đủ thông tin trước khi đăng ký homestay. Các trường còn thiếu: " + string.Join(", ", missingFields) + ".";
                return Json(new { success = false, message, redirectUrl = Url.Action("Profile", "User") });
            }

            return Json(new { success = true, message = "Thông tin người dùng đã đầy đủ." });
        }
        // Kiểm tra hợp đồng đang chờ duyệt
        [HttpGet]
        public async Task<IActionResult> CheckPendingContracts()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy người dùng." });
            }

            try
            {
                var hasPendingContract = await _hopDongRepository.GetHopDongQuery()
                    .AnyAsync(hd => hd.Ma_ND == user.Id && hd.TrangThai == "Đang chờ duyệt");

                return Json(new { success = true, hasPendingContract });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi kiểm tra hợp đồng: " + ex.Message });
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetAvailableServices(string maPDPhong)
        {
            try
            {
                // Kiểm tra người dùng
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng." });
                }

                // Lấy phiếu đặt phòng
                var phieu = await _phieuDatPhongRepository.GetByIdAsync(maPDPhong);
                if (phieu == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy phiếu đặt phòng." });
                }

                if (phieu.Ma_ND != user.Id)
                {
                    return Json(new { success = false, message = "Bạn không có quyền truy cập phiếu này." });
                }

                if (phieu.TrangThai != "Đã xác nhận")
                {
                    return Json(new { success = false, message = "Chỉ có thể thêm dịch vụ cho phiếu đã xác nhận." });
                }

                // Lấy ID homestay từ phòng đầu tiên (giả định tất cả phòng cùng homestay)
                var idHomestay = phieu.ChiTietDatPhongs?.FirstOrDefault()?.Phong?.Homestay?.ID_Homestay;
                if (string.IsNullOrEmpty(idHomestay))
                {
                    return Json(new { success = false, message = "Không thể xác định homestay cho phiếu này." });
                }

                // Lấy danh sách dịch vụ của homestay
                var dichVus = await _serviceRepository.GetMinimalByHomestayAsync(idHomestay);
                var allServices = dichVus.Select(dv => new
                {
                    maDv = dv.Ma_DV,
                    tenDv = dv.Ten_DV,
                    donGia = dv.DonGia
                }).ToList();

                // Lấy danh sách dịch vụ đã sử dụng cho từng phòng
                var rooms = phieu.ChiTietDatPhongs.Select(ct => new
                {
                    maPhong = ct.Ma_Phong,
                    tenPhong = ct.Phong?.TenPhong ?? "Không xác định",
                    usedServiceIds = (ct.PhieuSuDungDVs ?? Enumerable.Empty<PhieuSuDungDV>())
                        .SelectMany(ps => ps.ChiTietPhieuDVs ?? Enumerable.Empty<ChiTietPhieuDV>())
                        .Select(ctdv => ctdv.Ma_DV)
                        .Distinct()
                        .ToList()
                }).ToList();

                // Lọc dịch vụ chưa sử dụng cho từng phòng
                var roomServices = rooms.Select(room => new
                {
                    room.maPhong,
                    room.tenPhong,
                    services = allServices
                        .Where(s => !room.usedServiceIds.Contains(s.maDv))
                        .Select(s => new
                        {
                            s.maDv,
                            s.tenDv,
                            s.donGia
                        })
                        .ToList()
                }).ToList();

                return Json(new
                {
                    success = true,
                    rooms = roomServices
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi server: " + ex.Message });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddService([FromBody] AddServiceViewModel model)
        {
            try
            {
                // Kiểm tra dữ liệu đầu vào
                if (model == null || string.IsNullOrEmpty(model.MaPDPhong) || model.Rooms == null || !model.Rooms.Any())
                {
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
                }

                // Kiểm tra người dùng
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng." });
                }

                // Kiểm tra phiếu đặt phòng
                var phieu = await _phieuDatPhongRepository.GetByIdAsync(model.MaPDPhong);
                if (phieu == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy phiếu đặt phòng." });
                }

                if (phieu.Ma_ND != user.Id)
                {
                    return Json(new { success = false, message = "Bạn không có quyền thêm dịch vụ cho phiếu này." });
                }

                if (phieu.TrangThai != "Đã xác nhận")
                {
                    return Json(new { success = false, message = "Chỉ có thể thêm dịch vụ cho phiếu đã xác nhận." });
                }

                // Kiểm tra các phòng thuộc phiếu đặt
                var validRoomIds = phieu.ChiTietDatPhongs.Select(ct => ct.Ma_Phong).ToList();
                var invalidRooms = model.Rooms.Where(r => !validRoomIds.Contains(r.MaPhong)).ToList();
                if (invalidRooms.Any())
                {
                    return Json(new { success = false, message = "Một hoặc nhiều phòng không thuộc phiếu đặt phòng này." });
                }

                // Lấy ID homestay
                var idHomestay = phieu.ChiTietDatPhongs.FirstOrDefault()?.Phong?.Homestay?.ID_Homestay;
                if (string.IsNullOrEmpty(idHomestay))
                {
                    return Json(new { success = false, message = "Không thể xác định homestay cho phiếu này." });
                }

                // Kiểm tra dịch vụ hợp lệ
                var allServiceIds = model.Rooms.SelectMany(r => r.Services.Select(s => s.MaDv)).Distinct().ToList();
                var validServices = await _serviceRepository.GetMinimalByHomestayAsync(idHomestay);
                var validServiceIds = validServices.Select(dv => dv.Ma_DV).ToList();

                var invalidServices = allServiceIds.Except(validServiceIds).ToList();
                if (invalidServices.Any())
                {
                    return Json(new { success = false, message = "Một hoặc nhiều dịch vụ không hợp lệ hoặc không thuộc homestay này." });
                }

                // Kiểm tra dịch vụ đã sử dụng cho từng phòng
                foreach (var room in model.Rooms)
                {
                    if (!room.Services.Any())
                    {
                        continue; // Bỏ qua phòng không có dịch vụ
                    }

                    var usedServiceIds = phieu.ChiTietDatPhongs
                        .Where(ct => ct.Ma_Phong == room.MaPhong)
                        .SelectMany(ct => ct.PhieuSuDungDVs ?? Enumerable.Empty<PhieuSuDungDV>())
                        .SelectMany(ps => ps.ChiTietPhieuDVs ?? Enumerable.Empty<ChiTietPhieuDV>())
                        .Select(ct => ct.Ma_DV)
                        .Distinct()
                        .ToList();

                    var alreadyUsedServices = room.Services.Select(s => s.MaDv).Intersect(usedServiceIds).ToList();
                    if (alreadyUsedServices.Any())
                    {
                        return Json(new { success = false, message = $"Một hoặc nhiều dịch vụ đã được đăng ký cho phòng {room.MaPhong}." });
                    }

                    foreach (var service in room.Services)
                    {
                        if (service.SoLuong <= 0)
                        {
                            return Json(new { success = false, message = $"Số lượng dịch vụ {service.MaDv} cho phòng {room.MaPhong} phải lớn hơn 0." });
                        }
                    }
                }

                // Xử lý thêm dịch vụ
                foreach (var room in model.Rooms)
                {
                    if (!room.Services.Any())
                    {
                        continue; // Bỏ qua phòng không có dịch vụ
                    }

                    // Tìm hoặc tạo PhieuSuDungDV
                    var chiTietDatPhong = phieu.ChiTietDatPhongs.First(ct => ct.Ma_Phong == room.MaPhong);
                    var phieuSuDungDV = chiTietDatPhong.PhieuSuDungDVs?.FirstOrDefault();

                    if (phieuSuDungDV == null)
                    {
                        phieuSuDungDV = new PhieuSuDungDV
                        {
                            Ma_Phieu = "PSDV-" + Guid.NewGuid().ToString(),
                            Ma_PDPhong = model.MaPDPhong,
                            Ma_Phong = room.MaPhong,
                            ChiTietPhieuDVs = new List<ChiTietPhieuDV>()
                        };
                        if (chiTietDatPhong.PhieuSuDungDVs == null)
                        {
                            chiTietDatPhong.PhieuSuDungDVs = new List<PhieuSuDungDV>();
                        }
                        chiTietDatPhong.PhieuSuDungDVs.Add(phieuSuDungDV);
                    }

                    // Thêm ChiTietPhieuDV
                    foreach (var service in room.Services)
                    {
                        var chiTietPhieuDV = new ChiTietPhieuDV
                        {
                            Ma_Phieu = phieuSuDungDV.Ma_Phieu,
                            Ma_DV = service.MaDv,
                            SoLuong = service.SoLuong,
                            NgaySuDung = DateTime.Now,
                            ID_Homestay = idHomestay
                        };
                        phieuSuDungDV.ChiTietPhieuDVs.Add(chiTietPhieuDV);
                    }
                }

                // Lưu thay đổi
                await _phieuDatPhongRepository.UpdateAsync(phieu);

                return Json(new { success = true, message = "Thêm dịch vụ thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi thêm dịch vụ: " + ex.Message });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBooking([FromBody] CancelBookingViewModel model)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng." });
                }

                var phieu = await _phieuDatPhongRepository.GetByIdAsync(model.MaPDPhong);
                if (phieu == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy phiếu đặt phòng." });
                }

                if (phieu.Ma_ND != user.Id)
                {
                    return Json(new { success = false, message = "Bạn không có quyền hủy phiếu này." });
                }

                if (phieu.TrangThai != "Chờ xác nhận" && phieu.TrangThai != "Đã xác nhận")
                {
                    return Json(new { success = false, message = "Chỉ có thể hủy phiếu ở trạng thái 'Chờ xác nhận' hoặc 'Đã xác nhận'." });
                }

                // Kiểm tra xem đã có phiếu hủy chưa
                var hasCancellation = await _huyPhongRepository.HasCancellationAsync(model.MaPDPhong);
                if (hasCancellation)
                {
                    return Json(new { success = false, message = "Phiếu đặt phòng này đã được hủy trước đó." });
                }

                //// Kiểm tra chính sách hủy (giả sử hủy trước 72 giờ so với ngày đến)
                var firstChiTiet = phieu.ChiTietDatPhongs?.FirstOrDefault();
                if (firstChiTiet != null && firstChiTiet.NgayDen < DateTime.Now.AddHours(72))
                {
                    return Json(new { success = false, message = "Không thể hủy vì đã quá thời gian cho phép (trước 72 giờ)." });
                }

                // Tạo phiếu hủy phòng
                var phieuHuyPhong = new PhieuHuyPhong
                {
                    MaPHP = "PHP-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                    Ma_PDPhong = model.MaPDPhong,
                    LyDo = model.LyDo ?? "Không có lý do cụ thể",
                    TenNganHang = model.TenNganHang,
                    SoTaiKhoan = model.SoTaiKhoan,
                    NgayHuy = DateTime.Now,
                    NguoiHuy = user.FullName,
                    TrangThai = "Chờ xử lý"
                };

                // Cập nhật trạng thái phiếu đặt phòng
                phieu.TrangThai = "Đã hủy";
                await _phieuDatPhongRepository.UpdateAsync(phieu);

                // Lưu phiếu hủy phòng
                await _huyPhongRepository.AddAsync(phieuHuyPhong);
                var hoaDon = await _hoaDonRepository.GetByPhieuDatPhongAsync(model.MaPDPhong);
                if (hoaDon != null)
                {
                    await _hoaDonRepository.DeleteAsync(hoaDon.Ma_HD);
                }

                return Json(new { success = true, message = "Hủy phiếu đặt phòng thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi hủy phiếu: " + ex.Message });
            }
        }
        //kiểm tra đã đánh giá chưa
        [HttpGet]
        public async Task<IActionResult> CheckReview(string maPDPhong)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng." });
                }
                var phieu = await _phieuDatPhongRepository.GetByIdAsync(maPDPhong);
                if (phieu == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy phiếu đặt phòng." });
                }
                if (phieu.Ma_ND != user.Id)
                {
                    return Json(new { success = false, message = "Bạn không có quyền kiểm tra đánh giá cho phiếu này." });
                }
                var hasReview = await _danhGiaRepository.ExistsAsync(user.Id, phieu.Ma_PDPhong);
                return Json(new { success = true, hasReview });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi kiểm tra đánh giá: " + ex.Message });
            }
        }
        // Xem danh đánh giá
        [HttpGet]
        public async Task<IActionResult> GetReviews(string maPDPhong)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng." });
                }
                var phieu = await _phieuDatPhongRepository.GetByIdAsync(maPDPhong);
                if (phieu == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy phiếu đặt phòng." });
                }
                if (phieu.Ma_ND != user.Id)
                {
                    return Json(new { success = false, message = "Bạn không có quyền xem đánh giá cho phiếu này." });
                }
                var reviews = await _danhGiaRepository.GetByMapdpAsync(maPDPhong);
                var danhGiaDto = new DanhGiaViewModel
                {
                    Ma_ND = reviews.Ma_ND,
                    ID_Homestay = reviews.ID_Homestay,
                    Ma_PDPhong = reviews.Ma_PDPhong,
                    BinhLuan = reviews.BinhLuan,
                    NgayDanhGia = reviews.NgayDanhGia,
                    HinhAnh = reviews.HinhAnh,
                    Rating = reviews.Rating,
                    TenNguoiDung = reviews.NguoiDung?.FullName ?? "Không xác định",
                    TenHomestay = reviews.Homestay?.Ten_Homestay ?? "Không xác định"
                };
                return Json(new { success = true, danhGiaDto });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi lấy đánh giá: " + ex.Message });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(AddReviewViewModel model)
        {
            try
            {
                // Kiểm tra người dùng
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng." });
                }

                // Kiểm tra phiếu đặt phòng
                var phieu = await _phieuDatPhongRepository.GetByIdAsync(model.MaPDPhong);
                if (phieu == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy phiếu đặt phòng." });
                }

                if (phieu.Ma_ND != user.Id)
                {
                    return Json(new { success = false, message = "Bạn không có quyền đánh giá phiếu này." });
                }

                // Kiểm tra trạng thái phiếu đặt phòng (chỉ cho phép đánh giá nếu đã hoàn thành)
                if (phieu.TrangThai != "Hoàn thành")
                {
                    return Json(new { success = false, message = "Chỉ có thể đánh giá phiếu đã hoàn thành." });
                }

                // Kiểm tra xem đã đánh giá chưa
                var existingReview = await _danhGiaRepository.ExistsAsync(user.Id,phieu.Ma_PDPhong);
                if (existingReview)
                {
                    return Json(new { success = false, message = "Bạn đã đánh giá phiếu đặt phòng này." });
                }

                // Kiểm tra rating hợp lệ
                if (model.Rating < 1 || model.Rating > 5)
                {
                    return Json(new { success = false, message = "Điểm đánh giá phải từ 1 đến 5." });
                }

                // Xử lý upload hình ảnh (nếu có)
                string? imageUrl = null;
                if (model.HinhAnh != null)
                {
                    var validTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/jpg" };
                    if (!validTypes.Contains(model.HinhAnh.ContentType))
                    {
                        return Json(new { success = false, message = "Chỉ chấp nhận file ảnh (JPEG, PNG, GIF)." });
                    }

                    if (model.HinhAnh.Length > 5 * 1024 * 1024)
                    {
                        return Json(new { success = false, message = "Kích thước file tối đa là 5MB." });
                    }

                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/reviews");
                    Directory.CreateDirectory(uploadsFolder);

                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(model.HinhAnh.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.HinhAnh.CopyToAsync(stream);
                    }

                    imageUrl = $"/uploads/reviews/{fileName}";
                }

                // Lấy ID_Homestay từ phiếu đặt phòng
                var idHomestay = phieu.ChiTietDatPhongs?.FirstOrDefault()?.Phong?.Homestay?.ID_Homestay;
                if (string.IsNullOrEmpty(idHomestay))
                {
                    return Json(new { success = false, message = "Không thể xác định homestay cho phiếu này." });
                }

                // Tạo đối tượng đánh giá
                var danhGia = new DanhGia
                {
                    Ma_ND = user.Id,
                    Ma_PDPhong = model.MaPDPhong,
                    ID_Homestay = idHomestay,
                    BinhLuan = model.BinhLuan,
                    Rating = model.Rating,
                    NgayDanhGia = DateTime.Now,
                    HinhAnh = imageUrl
                };

                // Lưu đánh giá
                await _danhGiaRepository.AddAsync(danhGia);

                return Json(new { success = true, message = "Đánh giá của bạn đã được gửi thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi khi gửi đánh giá: {ex.Message}" });
            }
        }
    }
}
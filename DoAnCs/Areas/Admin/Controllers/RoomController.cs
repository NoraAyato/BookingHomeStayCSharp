using DoAnCs.Models;
using DoAnCs.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DoAnCs.Areas.Admin.Controllers
{
    [Route("Admin/Room")] 
    public class RoomController : BaseController
    {
        private readonly IPhongRepository _phongRepo;
        private readonly IServiceRepository _dichVuRepo;
        private readonly ApplicationDbContext _context;
        private readonly IHomestayRepository _homeRepo;
        private readonly ITienNghiRepository _tienNghiRepo;
        public RoomController(IPhongRepository phongRepo, IServiceRepository dichVuRepo, ApplicationDbContext context, IHomestayRepository homeRepo, ITienNghiRepository tienNghiRepo)
        {
            _phongRepo = phongRepo;
            _dichVuRepo = dichVuRepo;
            _context = context;
            _homeRepo = homeRepo;
            _tienNghiRepo = tienNghiRepo;
        }

        // GET: Admin/Room hoặc Admin/Room/Index
        [HttpGet]
        public async Task<IActionResult> Index(string homestayId)
        {
            if (string.IsNullOrEmpty(homestayId))
                return BadRequest("HomestayId is required");

            var homestay = await _homeRepo.GetByIdWithDetailsAsync(homestayId);
            if (homestay == null)
                return NotFound("Homestay not found");

            var rooms = await _phongRepo.GetByHomestayAsync(homestayId);
            ViewBag.HomestayId = homestayId;
            ViewBag.HomestayName = homestay.Ten_Homestay;
            ViewBag.LoaiPhongList = await _phongRepo.GetLoaiPhongsAsync()
                .ContinueWith(t => t.Result.Select(l => new { Value = l.ID_Loai, Text = l.TenLoai }));
            ViewBag.TienNghiList = await _tienNghiRepo.GetAllAsValueTextAsync();

            return View(rooms);
        }

      
    
        // POST: Admin/Room/Create
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Phong phong, List<IFormFile> HinhAnhPhongFiles, List<ChiTietPhong> ChiTietPhongs, int MainImageIndex = 0, List<string> ImageDescriptions = null)
        {
            try
            {
                var homestayExists = await _homeRepo.ExistsAsync(phong.ID_Homestay);
                if (!homestayExists)
                {
                    return Json(new { success = false, message = $"ID_Homestay '{phong.ID_Homestay}' không tồn tại" });
                }

                var loaiPhongExists = await _phongRepo.ExistsLoaiPhongAsync(phong.ID_Loai);
                if (!loaiPhongExists)
                {
                    return Json(new { success = false, message = $"ID_Loai '{phong.ID_Loai}' không tồn tại" });
                }

                if (ChiTietPhongs != null && ChiTietPhongs.Any())
                {
                    var maTienNghiList = ChiTietPhongs.Select(ct => ct.Ma_TienNghi).ToList();
                    if (maTienNghiList.Distinct().Count() != maTienNghiList.Count)
                    {
                        return Json(new { success = false, message = "Có tiện nghi bị trùng lặp trong danh sách" });
                    }

                    foreach (var chiTiet in ChiTietPhongs)
                    {

                        if (!await _tienNghiRepo.ExistsAsync(chiTiet.Ma_TienNghi))
                        {
                            return Json(new { success = false, message = $"Tiện nghi với mã '{chiTiet.Ma_TienNghi}' không tồn tại" });
                        }
                    }
                }
                // Kiểm tra VirtualTour
                if (!string.IsNullOrEmpty(phong.VirtualTour) && phong.VirtualTour.Length > 500)
                {
                    return Json(new { success = false, message = "Đường dẫn VirtualTour không được vượt quá 500 ký tự" });
                }

                phong.Ma_Phong = "R" + Guid.NewGuid().ToString("N").Substring(0, 19);

                if (await _phongRepo.ExistsPhongAsync(phong.Ma_Phong))
                {
                    return Json(new { success = false, message = "Mã phòng đã tồn tại, thử lại" });
                }

                _phongRepo.AddAsync(phong);

                if (HinhAnhPhongFiles != null && HinhAnhPhongFiles.Any())
                {
                    var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                    if (!Directory.Exists(uploadDir))
                    {
                        Directory.CreateDirectory(uploadDir);
                    }

                    for (int i = 0; i < HinhAnhPhongFiles.Count; i++)
                    {
                        var file = HinhAnhPhongFiles[i];
                        if (file.Length > 0)
                        {
                            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                            var filePath = Path.Combine(uploadDir, fileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            string moTa = ImageDescriptions != null && i < ImageDescriptions.Count ? ImageDescriptions[i] : null;

                            _context.HinhAnhPhongs.Add(new HinhAnhPhong
                            {
                                MaPhong = phong.Ma_Phong,
                                UrlAnh = "/uploads/" + fileName,
                                MoTa = moTa,
                                LaAnhChinh = i == MainImageIndex
                            });
                        }
                    }
                }

                if (ChiTietPhongs != null && ChiTietPhongs.Any())
                {
                    foreach (var chiTiet in ChiTietPhongs.Where(ct => !string.IsNullOrEmpty(ct.Ma_TienNghi) && ct.SoLuong > 0))
                    {
                        chiTiet.Ma_Phong = phong.Ma_Phong;
                        _context.ChiTietPhongs.Add(chiTiet);
                    }
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Thêm phòng thành công" });
            }
            catch (DbUpdateException ex)
            {
                var innerExceptionMessage = ex.InnerException?.Message ?? ex.Message;
                return Json(new { success = false, message = $"Lỗi khi lưu vào cơ sở dữ liệu: {innerExceptionMessage}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi khi thêm phòng: {ex.Message}" });
            }
        }

        // POST: Admin/Room/Update
        [HttpPost("Update")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(Phong phong, List<IFormFile> HinhAnhPhongFiles, List<ChiTietPhong> ChiTietPhongs, List<string> ImageDescriptions, int MainImageIndex = 0)
        {
            var existingRoom = await _context.Phongs
                .Include(p => p.HinhAnhPhongs)
                .Include(p => p.ChiTietPhongs)
                .FirstOrDefaultAsync(p => p.Ma_Phong == phong.Ma_Phong);

            if (existingRoom == null)
            {
                return Json(new { success = false, message = "Phòng không tồn tại" });
            }

            try
            {
                if (ChiTietPhongs != null && ChiTietPhongs.Any())
                {
                    var maTienNghiList = ChiTietPhongs.Select(ct => ct.Ma_TienNghi).ToList();
                    if (maTienNghiList.Distinct().Count() != maTienNghiList.Count)
                    {
                        return Json(new { success = false, message = "Có tiện nghi bị trùng lặp trong danh sách" });
                    }

                    foreach (var chiTiet in ChiTietPhongs)
                    {
                        if (!await _context.TienNghis.AnyAsync(t => t.Ma_TienNghi == chiTiet.Ma_TienNghi))
                        {
                            return Json(new { success = false, message = $"Tiện nghi với mã '{chiTiet.Ma_TienNghi}' không tồn tại" });
                        }
                    }
                }
                // Kiểm tra VirtualTour
                if (!string.IsNullOrEmpty(phong.VirtualTour) && phong.VirtualTour.Length > 500)
                {
                    return Json(new { success = false, message = "Đường dẫn VirtualTour không được vượt quá 500 ký tự" });
                }
                existingRoom.TenPhong = phong.TenPhong;
                existingRoom.ID_Loai = phong.ID_Loai;
                existingRoom.DonGia = phong.DonGia;
                existingRoom.SoNguoi = phong.SoNguoi;
                existingRoom.TrangThai = phong.TrangThai;
                existingRoom.VirtualTour = phong.VirtualTour;
                if (HinhAnhPhongFiles != null && HinhAnhPhongFiles.Any())
                {
                    if (existingRoom.HinhAnhPhongs.Any())
                    {
                        foreach (var img in existingRoom.HinhAnhPhongs)
                        {
                            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", img.UrlAnh.TrimStart('/'));
                            if (System.IO.File.Exists(filePath))
                            {
                                System.IO.File.Delete(filePath);
                            }
                        }
                        _context.HinhAnhPhongs.RemoveRange(existingRoom.HinhAnhPhongs);
                    }

                    for (int i = 0; i < HinhAnhPhongFiles.Count; i++)
                    {
                        var file = HinhAnhPhongFiles[i];
                        if (file.Length > 0)
                        {
                            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads", fileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            var description = (i < ImageDescriptions.Count) ? ImageDescriptions[i] : (i == MainImageIndex ? "Hình chính" : "Hình phụ");

                            _context.HinhAnhPhongs.Add(new HinhAnhPhong
                            {
                                MaPhong = phong.Ma_Phong,
                                UrlAnh = "/uploads/" + fileName,
                                MoTa = description,
                                LaAnhChinh = i == MainImageIndex
                            });
                        }
                    }
                }
                else if (ImageDescriptions != null && ImageDescriptions.Any() && existingRoom.HinhAnhPhongs.Any())
                {
                    var existingImages = existingRoom.HinhAnhPhongs.ToList();
                    for (int i = 0; i < existingImages.Count; i++)
                    {
                        if (i < ImageDescriptions.Count)
                        {
                            existingImages[i].MoTa = ImageDescriptions[i];
                            existingImages[i].LaAnhChinh = i == MainImageIndex;
                        }
                        else
                        {
                            existingImages[i].MoTa = i == MainImageIndex ? "Hình chính" : "Hình phụ";
                            existingImages[i].LaAnhChinh = i == MainImageIndex;
                        }
                    }
                }

                if (existingRoom.ChiTietPhongs.Any())
                {
                    _context.ChiTietPhongs.RemoveRange(existingRoom.ChiTietPhongs);
                }

                if (ChiTietPhongs != null && ChiTietPhongs.Any())
                {
                    foreach (var chiTiet in ChiTietPhongs.Where(ct => !string.IsNullOrEmpty(ct.Ma_TienNghi) && ct.SoLuong > 0))
                    {
                        chiTiet.Ma_Phong = phong.Ma_Phong;
                        _context.ChiTietPhongs.Add(chiTiet);
                    }
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Cập nhật phòng thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi cập nhật phòng: " + ex.Message });
            }
        }

        // GET: Admin/Room/GetRoom
        [HttpGet("GetRoom")]
        public async Task<IActionResult> GetRoom(string id)
        {
            var room = await _phongRepo.GetByIdAsync(id);
            if (room == null)
                return Json(new { success = false, message = "Phòng không tồn tại" });

            return Json(new
            {
                success = true,
                data = new
                {
                    ma_Phong = room.Ma_Phong,
                    tenPhong = room.TenPhong,
                    id_Loai = room.ID_Loai,
                    donGia = room.DonGia,
                    soNguoi = room.SoNguoi,
                    trangThai = room.TrangThai,
                    hinhAnhPhongs = room.HinhAnhPhongs?.Select(h => new
                    {
                        urlAnh = h.UrlAnh,
                        laAnhChinh = h.LaAnhChinh
                    }),
                    chiTietPhongs = room.ChiTietPhongs?.Select(c => new
                    {
                        ma_TienNghi = c.Ma_TienNghi,
                        tenTienNghi = c.TienNghi?.TenTienNghi,
                        soLuong = c.SoLuong
                    })
                }
            });
        }

        // POST: Admin/Room/DeleteConfirmed
        [HttpPost("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                    return Json(new { success = false, message = "Mã phòng không hợp lệ" });

                var phong = await _context.Phongs
                    .Include(p => p.HinhAnhPhongs)
                    .Include(p => p.ChiTietPhongs)
                    .FirstOrDefaultAsync(p => p.Ma_Phong == id);

                if (phong == null)
                    return Json(new { success = false, message = "Phòng không tồn tại" });

                if (phong.HinhAnhPhongs != null)
                {
                    foreach (var img in phong.HinhAnhPhongs)
                    {
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", img.UrlAnh.TrimStart('/'));
                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                        }
                    }
                }
   

                await _phongRepo.DeleteAsync(phong.Ma_Phong);

                return Json(new { success = true, message = "Xóa phòng thành công" });
            }
            catch (Exception ex)
            {
                var errorMessage = ex.InnerException?.Message ?? ex.Message;
                return Json(new { success = false, message = $"Lỗi khi xóa phòng: {errorMessage}" });
            }
        }

        private bool IsValidImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return false;

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            return allowedExtensions.Contains(extension);
        }
    }
}
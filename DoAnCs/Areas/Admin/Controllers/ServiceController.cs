using DoAnCs.Models;
using DoAnCs.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DoAnCs.Areas.Admin.Controllers
{
    public class ServiceController : BaseController
    {
        private readonly IServiceRepository _dichVuRepo;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ServiceController> _logger;

        // Danh sách dịch vụ mẫu
        private static readonly List<string> ServiceTemplates = new List<string>
        {
            "Wi-Fi miễn phí",
            "Ăn uống",
            "Giặt Ủi",
            "Thuê Xe",
            "Hướng dẫn viên du lịch",
            "Bể bơi",
            "Gym",
            "Spa"
        };

        public ServiceController(IServiceRepository dichVuRepo, ApplicationDbContext context, ILogger<ServiceController> logger)
        {
            _dichVuRepo = dichVuRepo;
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string homestayId, int pageNumber = 1, int pageSize = 10)
        {
            if (string.IsNullOrEmpty(homestayId))
            {
                return BadRequest("homestayId không được để trống");
            }

            ViewBag.HomestayId = homestayId;
            ViewBag.PageNumber = pageNumber;
            ViewBag.PageSize = pageSize;

            var dichVus = await _dichVuRepo.GetByHomestayAsync(homestayId, pageNumber, pageSize);
            var totalDichVus = await _dichVuRepo.CountByHomestayAsync(homestayId);

            ViewBag.TotalDichVus = totalDichVus;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalDichVus / pageSize);

            var homestay = await _context.Homestays.FindAsync(homestayId);
            ViewBag.HomestayName = homestay?.Ten_Homestay ?? "Không tìm thấy homestay";

            // Truyền danh sách dịch vụ mẫu để sử dụng trong JavaScript
            ViewBag.ServiceTemplates = ServiceTemplates;

            return View(dichVus);
        }

        [HttpPost]
        public async Task<IActionResult> Create(DichVu model, IFormFile HinhAnhFile)
        {
            try
            {
                if (string.IsNullOrEmpty(model.ID_Homestay))
                    return Json(new { success = false, message = "ID_Homestay không được để trống" });
                if (string.IsNullOrEmpty(model.Ten_DV))
                    return Json(new { success = false, message = "Tên dịch vụ không được để trống" });
                if (!ServiceTemplates.Contains(model.Ten_DV))
                    return Json(new { success = false, message = "Tên dịch vụ không hợp lệ" });
                if (model.DonGia <= 0)
                    return Json(new { success = false, message = "Đơn giá phải lớn hơn 0" });

                var homestayExists = await _context.Homestays.AnyAsync(h => h.ID_Homestay == model.ID_Homestay);
                if (!homestayExists)
                    return Json(new { success = false, message = "ID_Homestay không tồn tại trong bảng Homestays" });

                model.Ma_DV = Guid.NewGuid().ToString("N").Substring(0, 20);

                if (HinhAnhFile != null && HinhAnhFile.Length > 0)
                {
                    var uploadsDir = Path.Combine("wwwroot", "img", "Services");
                    if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);

                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(HinhAnhFile.FileName)}";
                    var filePath = Path.Combine(uploadsDir, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await HinhAnhFile.CopyToAsync(stream);
                        _logger.LogInformation("Saved service image to: {FilePath}", filePath);
                    }
                    model.HinhAnh = $"/img/Services/{fileName}";
                }

                await _dichVuRepo.AddAsync(model);
                return Json(new { success = true, message = "Thêm dịch vụ thành công" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo dịch vụ: {Message}", ex.Message);
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo dịch vụ: {Message}", ex.Message);
                return Json(new { success = false, message = "Có lỗi xảy ra khi thêm dịch vụ" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Update(string id, DichVu model, IFormFile HinhAnhFile)
        {
            if (id != model.Ma_DV)
            {
                return Json(new { success = false, message = "ID không khớp" });
            }

            try
            {
                var existingDichVu = await _dichVuRepo.GetByIdAsync(id);
                if (existingDichVu == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy dịch vụ" });
                }

                if (string.IsNullOrEmpty(model.Ten_DV))
                    return Json(new { success = false, message = "Tên dịch vụ không được để trống" });
                if (!ServiceTemplates.Contains(model.Ten_DV))
                    return Json(new { success = false, message = "Tên dịch vụ không hợp lệ" });
                if (model.DonGia <= 0)
                    return Json(new { success = false, message = "Đơn giá phải lớn hơn 0" });

                existingDichVu.Ten_DV = model.Ten_DV;
                existingDichVu.DonGia = model.DonGia;
                existingDichVu.ID_Homestay = model.ID_Homestay;

                if (HinhAnhFile != null && HinhAnhFile.Length > 0)
                {
                    if (!string.IsNullOrEmpty(existingDichVu.HinhAnh))
                    {
                        var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existingDichVu.HinhAnh.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                            _logger.LogInformation("Deleted old service image: {FilePath}", oldFilePath);
                        }
                    }

                    var uploadsDir = Path.Combine("wwwroot", "img", "Services");
                    if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);

                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(HinhAnhFile.FileName)}";
                    var filePath = Path.Combine(uploadsDir, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await HinhAnhFile.CopyToAsync(stream);
                        _logger.LogInformation("Saved service image to: {FilePath}", filePath);
                    }
                    existingDichVu.HinhAnh = $"/img/Services/{fileName}";
                }

                await _dichVuRepo.UpdateAsync(existingDichVu);
                return Json(new { success = true, message = "Cập nhật dịch vụ thành công" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật dịch vụ: {Message}", ex.Message);
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật dịch vụ: {Message}", ex.Message);
                return Json(new { success = false, message = "Có lỗi xảy ra khi cập nhật dịch vụ" });
            }
        }

        [HttpPost, ActionName("DeleteConfirmed")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            try
            {
                await _dichVuRepo.DeleteAsync(id);
                return Json(new { success = true, message = "Xóa dịch vụ thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa dịch vụ: {Message}", ex.Message);
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa dịch vụ" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDichVu(string id)
        {
            var dichVu = await _dichVuRepo.GetByIdAsync(id);
            if (dichVu == null)
            {
                return NotFound();
            }

            var result = new
            {
                ma_DV = dichVu.Ma_DV,
                ten_DV = dichVu.Ten_DV,
                donGia = dichVu.DonGia,
                hinhAnh = dichVu.HinhAnh,
                id_Homestay = dichVu.ID_Homestay
            };

            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailableServiceTemplates(string homestayId, string excludeMaDV = null)
        {
            try
            {
                var usedServiceNames = await _context.DichVus
                    .Where(dv => dv.ID_Homestay == homestayId && (excludeMaDV == null || dv.Ma_DV != excludeMaDV))
                    .Select(dv => dv.Ten_DV)
                    .ToListAsync();

                var availableTemplates = ServiceTemplates
                    .Where(template => !usedServiceNames.Contains(template))
                    .ToList();

                return Json(availableTemplates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách dịch vụ mẫu: {Message}", ex.Message);
                return Json(new { success = false, message = "Có lỗi xảy ra khi lấy danh sách dịch vụ mẫu" });
            }
        }
    }
}
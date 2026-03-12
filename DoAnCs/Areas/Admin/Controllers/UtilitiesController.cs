using DoAnCs.Models;
using DoAnCs.Repository;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace DoAnCs.Areas.Admin.Controllers
{
    public class UtilitiesController : BaseController
    {
        private readonly ITienNghiRepository _tienNghiRepo;

        public UtilitiesController(ITienNghiRepository tienNghiRepo)
        {
            _tienNghiRepo = tienNghiRepo;
        }

        public async Task<IActionResult> Index()
        {
            var tienNghis = await _tienNghiRepo.GetAllAsync();
            return View(tienNghis);
        }

        [HttpPost]
        public async Task<IActionResult> Create(TienNghi tienNghi)
        {
            try
            {
                ModelState.Remove("Ma_TienNghi");
                ModelState.Remove("ChiTietPhongs");
                if (ModelState.IsValid)
                {
                    // Generate Ma_TienNghi automatically
                    var count = await _tienNghiRepo.CountAsync();
                    tienNghi.Ma_TienNghi = $"TN{count + 1:D3}"; // e.g., TN001, TN002, ...

                    await _tienNghiRepo.AddAsync(tienNghi);
                    return Json(new { success = true, message = "Thêm tiện nghi thành công" });
                }

                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return Json(new { success = false, message = "Dữ liệu không hợp lệ", errors });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTienNghi(string id)
        {
            var tienNghi = await _tienNghiRepo.GetByIdAsync(id);
            if (tienNghi == null)
            {
                return NotFound();
            }

            return Json(new
            {
                ma_TienNghi = tienNghi.Ma_TienNghi,
                tenTienNghi = tienNghi.TenTienNghi,
                moTa = tienNghi.MoTa
            });
        }

        [HttpPost]
        public async Task<IActionResult> Update(string id, TienNghi tienNghi)
        {
            try
            {
                if (id != tienNghi.Ma_TienNghi)
                {
                    return Json(new { success = false, message = "Mã tiện nghi không khớp" });
                }

                var existingTienNghi = await _tienNghiRepo.GetByIdAsync(id);
                if (existingTienNghi == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy tiện nghi" });
                }

                if (ModelState.IsValid)
                {
                    existingTienNghi.TenTienNghi = tienNghi.TenTienNghi;
                    existingTienNghi.MoTa = tienNghi.MoTa;
                    await _tienNghiRepo.UpdateAsync(existingTienNghi);
                    return Json(new { success = true, message = "Cập nhật tiện nghi thành công" });
                }

                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return Json(new { success = false, message = "Dữ liệu không hợp lệ", errors });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            try
            {
                var tienNghi = await _tienNghiRepo.GetByIdAsync(id);
                if (tienNghi == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy tiện nghi" });
                }

                await _tienNghiRepo.DeleteAsync(id);
                return Json(new { success = true, message = "Xóa tiện nghi thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
using DoAnCs.Models;
using DoAnCs.Repository;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace DoAnCs.Areas.Admin.Controllers
{
    public class LocationController : BaseController
    {
        private readonly IKhuVucRepository _khuVucRepo;

        public LocationController(IKhuVucRepository khuVucRepo)
        {
            _khuVucRepo = khuVucRepo;
        }

        public async Task<IActionResult> Index(string searchString = null, int page = 1, int pageSize = 10)
        {
            var khuVucs = await _khuVucRepo.GetAllAsync();
            if (!string.IsNullOrEmpty(searchString))
            {
                khuVucs = khuVucs.Where(k => k.Ten_KV.Contains(searchString, StringComparison.OrdinalIgnoreCase));
            }

            var totalCount = khuVucs.Count();
            khuVucs = khuVucs
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = totalCount;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.SearchString = searchString;

            return View(khuVucs);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(KhuVuc model)
        {
            try
            {
                ModelState.Remove("Homestays"); // Đã thêm từ trước để bỏ qua validation Homestays
                ModelState.Remove("Ma_KV");
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ", errors });
                }
                var allKhuVuc = await _khuVucRepo.GetAllAsync();
                if (allKhuVuc.Any(kv => kv.Ten_KV.Trim().ToLower() == model.Ten_KV.Trim().ToLower()))
                {
                    return Json(new { success = false, message = "Tên khu vực đã tồn tại." });
                }
                if (await _khuVucRepo.ExistsAsync(model.Ma_KV))
                {
                    return Json(new { success = false, message = "Mã khu vực đã tồn tại" });
                }
                model.Ma_KV = await GenerateMaKV();
                model.Homestays ??= new List<Homestay>();
                await _khuVucRepo.AddAsync(model);
                return Json(new { success = true, message = "Thêm khu vực thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        private async Task<string> GenerateMaKV()
        {
            var allKhuVuc = await _khuVucRepo.GetAllAsync();
            int count = allKhuVuc.Count() + 1;
            string generatedMaKV;

            do
            {
                generatedMaKV = $"KV{count:D2}"; // Format as KV01, KV02, etc.
                count++;
            } while (allKhuVuc.Any(kv => kv.Ma_KV == generatedMaKV));

            return generatedMaKV;
        }
        public async Task<IActionResult> Update(string id)
        {
            var khuVuc = await _khuVucRepo.GetByIdAsync(id);
            if (khuVuc == null)
            {
                return NotFound();
            }

            return View(khuVuc);
        }

        [HttpGet]
        public async Task<IActionResult> IsTenKhuVucExist(string tenKV, string? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(tenKV))
                return Json(false);

            var allKhuVuc = await _khuVucRepo.GetAllAsync();
            var exists = allKhuVuc.Any(kv =>
                kv.Ten_KV.Trim().ToLower() == tenKV.Trim().ToLower() &&
                (excludeId == null || kv.Ma_KV != excludeId));

            return Json(!exists);
        }


        [HttpPost]
        public async Task<IActionResult> Update(string id, KhuVuc model)
        {
            if (id != model.Ma_KV)
            {
                return Json(new { success = false, message = "Mã khu vực không khớp" });
            }

            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ", errors });
                }
                var allKhuVuc = await _khuVucRepo.GetAllAsync();
                if (allKhuVuc.Any(kv => kv.Ten_KV.Trim().ToLower() == model.Ten_KV.Trim().ToLower()))
                {
                    return Json(new { success = false, message = "Tên khu vực đã tồn tại." });
                }
                var existingKhuVuc = await _khuVucRepo.GetByIdAsync(id);
                if (existingKhuVuc == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy khu vực" });
                }

                existingKhuVuc.Ten_KV = model.Ten_KV;
                await _khuVucRepo.UpdateAsync(existingKhuVuc);
                return Json(new { success = true, message = "Cập nhật khu vực thành công" });
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
                if (await _khuVucRepo.HasHomestaysAsync(id))
                {
                    return Json(new { success = false, message = "Không thể xóa khu vực vì có homestay liên kết" });
                }

                var khuVuc = await _khuVucRepo.GetByIdAsync(id);
                if (khuVuc == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy khu vực" });
                }

                await _khuVucRepo.DeleteAsync(id);
                return Json(new { success = true, message = "Xóa khu vực thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
      

        [HttpGet]
        public async Task<IActionResult> GetKhuVuc(string id)
        {
            var khuVuc = await _khuVucRepo.GetByIdAsync(id);
            if (khuVuc == null)
            {
                return NotFound();
            }

            return Json(new
            {
                ma_KV = khuVuc.Ma_KV,
                ten_KV = khuVuc.Ten_KV
            });
        }

        [HttpGet]
        public async Task<IActionResult> SearchByName(string term)
        {
            var results = await _khuVucRepo.SearchByNameAsync(term);
            return Json(results);
        }
    }
}
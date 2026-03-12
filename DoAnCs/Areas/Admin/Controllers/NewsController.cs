using DoAnCs.Models;
using DoAnCs.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DoAnCs.Areas.Admin.Controllers
{
    public class NewsController : BaseController
    {
        private readonly INewsRepository _newsRepo;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NewsController> _logger;

        public NewsController(INewsRepository newsRepo, ApplicationDbContext context, ILogger<NewsController> logger)
        {
            _newsRepo = newsRepo;
            _context = context;
            _logger = logger;
        }

        // Hiển thị giao diện quản lý tin tức
        public IActionResult Index()
        {
            return View();
        }

        // API lấy danh sách chủ đề
        [HttpGet]
        public async Task<IActionResult> GetChuDeList()
        {
            try
            {
                var chuDeList = await _context.ChuDes
                    .Select(c => new { Value = c.ID_ChuDe, Text = c.TenChuDe })
                    .ToListAsync();
                return Json(new { success = true, data = chuDeList });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetChuDeList");
                return Json(new { success = false, message = "Không thể lấy danh sách chủ đề" });
            }
        }

        // API lấy danh sách tin tức với lọc và phân trang
        [HttpGet]
        public async Task<IActionResult> GetNewsList(int page = 1, int pageSize = 9, string search = null, string topic = null, string status = null, string sort = "newest")
        {
            try
            {
                var query = _newsRepo.GetTinTucQueryable();

                // Áp dụng bộ lọc
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(n => n.TieuDe.Contains(search) || n.TacGia.Contains(search));
                }
                if (!string.IsNullOrEmpty(topic) && topic != "all")
                {
                    query = query.Where(n => n.ID_ChuDe == topic);
                }
                if (!string.IsNullOrEmpty(status) && status != "all")
                {
                    query = query.Where(n => n.TrangThai == status);
                }

                // Áp dụng sắp xếp
                query = sort == "newest" ? query.OrderByDescending(n => n.NgayDang) : query.OrderBy(n => n.NgayDang);

                // Tính tổng số bản ghi
                var totalItems = await query.CountAsync();

                // Phân trang
                var newsList = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return Json(new
                {
                    success = true,
                    data = newsList.Select(n => new
                    {
                        n.Ma_TinTuc,
                        n.ID_ChuDe,
                        n.TieuDe,
                        NoiDung = n.NoiDung.Length > 150 ? n.NoiDung.Substring(0, 150) + "..." : n.NoiDung,
                        n.HinhAnh,
                        n.TacGia,
                        NgayDang = n.NgayDang.ToString("dd/MM/yyyy"),
                        n.TrangThai,
                        ChuDe = n.ChuDe?.TenChuDe,
                        BinhLuanCount = n.BinhLuans?.Count ?? 0
                    }),
                    pagination = new
                    {
                        CurrentPage = page,
                        TotalPages = (int)Math.Ceiling((double)totalItems / pageSize),
                        PageSize = pageSize,
                        TotalItems = totalItems
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetNewsList");
                return Json(new { success = false, message = "Không thể lấy danh sách tin tức" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetNews(string id)
        {
            var news = await _newsRepo.GetTinTucByIdAsync(id);
            if (news == null)
            {
                return NotFound();
            }

            return Json(new
            {
                maTinTuc = news.Ma_TinTuc,
                idChuDe = news.ID_ChuDe,
                tieuDe = news.TieuDe,
                noiDung = news.NoiDung,
                hinhAnh = news.HinhAnh,
                tacGia = news.TacGia,
                ngayDang = news.NgayDang.ToString("yyyy-MM-dd"),
                trangThai = news.TrangThai
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TinTuc model, IFormFile HinhAnhFile)
        {
            try
            {
                model.Ma_TinTuc = "N" + Guid.NewGuid().ToString("N").Substring(0, 18);
                model.NgayDang = DateTime.UtcNow;

                if (HinhAnhFile != null && HinhAnhFile.Length > 0)
                {
                    // Kiểm tra loại tệp
                    var allowedTypes = new[] { "image/jpeg", "image/png" };
                    if (!allowedTypes.Contains(HinhAnhFile.ContentType))
                    {
                        return Json(new { success = false, message = "Chỉ hỗ trợ file PNG hoặc JPG" });
                    }

                    // Kiểm tra kích thước tệp (5MB)
                    if (HinhAnhFile.Length > 5 * 1024 * 1024)
                    {
                        return Json(new { success = false, message = "File quá lớn (tối đa 5MB)" });
                    }

                    var uploadsDir = Path.Combine("wwwroot", "img", "news");
                    if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);

                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(HinhAnhFile.FileName)}";
                    var filePath = Path.Combine(uploadsDir, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await HinhAnhFile.CopyToAsync(stream);
                    }

                    model.HinhAnh = $"/img/news/{fileName}";
                }

                await _newsRepo.AddTinTucAsync(model);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Create");
                return Json(new { success = false, message = "Có lỗi xảy ra khi thêm bài viết" });
            }
        }
        [HttpGet]
        public IActionResult CheckTopicName(string name, string excludeId = "")
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Json(new { exists = false });
            }

            var exists = _context.ChuDes
                .Any(cd => cd.TenChuDe.Trim().ToLower() == name.Trim().ToLower() && cd.ID_ChuDe != excludeId);

            return Json(new { exists });
        }
        [HttpGet]
        public IActionResult GetTopicNewsCount(string id)
        {
            try
            {
                var newsCount = _context.TinTucs.Count(t => t.ID_ChuDe == id);
                return Json(new { success = true, newsCount });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi truy vấn số lượng tin tức: " + ex.Message });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(string id, TinTuc model, IFormFile HinhAnhFile)
        {
            if (id != model.Ma_TinTuc)
            {
                return Json(new { success = false, message = "ID không khớp" });
            }

            try
            {
                var existingNews = await _newsRepo.GetTinTucByIdAsync(id);
                if (existingNews == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy tin tức" });
                }

                existingNews.ID_ChuDe = model.ID_ChuDe;
                existingNews.TieuDe = model.TieuDe;
                existingNews.NoiDung = model.NoiDung;
                existingNews.TacGia = model.TacGia;
                existingNews.TrangThai = model.TrangThai;

                if (HinhAnhFile != null && HinhAnhFile.Length > 0)
                {
                    // Kiểm tra loại tệp
                    var allowedTypes = new[] { "image/jpeg", "image/png" };
                    if (!allowedTypes.Contains(HinhAnhFile.ContentType))
                    {
                        return Json(new { success = false, message = "Chỉ hỗ trợ file PNG hoặc JPG" });
                    }

                    // Kiểm tra kích thước tệp (5MB)
                    if (HinhAnhFile.Length > 5 * 1024 * 1024)
                    {
                        return Json(new { success = false, message = "File quá lớn (tối đa 5MB)" });
                    }

                    var uploadsDir = Path.Combine("wwwroot", "img", "news");
                    if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);

                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(HinhAnhFile.FileName)}";
                    var filePath = Path.Combine(uploadsDir, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await HinhAnhFile.CopyToAsync(stream);
                    }

                    if (!string.IsNullOrEmpty(existingNews.HinhAnh) && System.IO.File.Exists(Path.Combine("wwwroot", existingNews.HinhAnh.TrimStart('/'))))
                    {
                        System.IO.File.Delete(Path.Combine("wwwroot", existingNews.HinhAnh.TrimStart('/')));
                    }

                    existingNews.HinhAnh = $"/img/news/{fileName}";
                }

                await _newsRepo.UpdateTinTucAsync(existingNews);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Update");
                return Json(new { success = false, message = "Có lỗi xảy ra khi cập nhật" });
            }
        }

        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            try
            {
                await _newsRepo.DeleteTinTucAsync(id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteConfirmed");
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa" });
            }
        }

        // Quản lý Bình luận
        public async Task<IActionResult> Comments(string id)
        {
            var news = await _newsRepo.GetTinTucByIdAsync(id);
            if (news == null)
            {
                return NotFound();
            }

            ViewBag.NewsTitle = news.TieuDe;
            ViewBag.NewsId = news.Ma_TinTuc;

            var comments = await _newsRepo.GetBinhLuansByTinTucAsync(id);
            return View(comments);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(int id)
        {
            try
            {
                await _newsRepo.DeleteBinhLuanAsync(id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteComment");
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa bình luận" });
            }
        }

        // Quản lý Chủ đề
        public async Task<IActionResult> Topics()
        {
            var topics = await _newsRepo.GetAllChuDeAsync();
            return View(topics);
        }

        [HttpGet]
        public async Task<IActionResult> GetTopic(string id)
        {
            var topic = await _newsRepo.GetChuDeByIdAsync(id);
            if (topic == null)
            {
                return NotFound();
            }

            return Json(new
            {
                idChuDe = topic.ID_ChuDe,
                tenChuDe = topic.TenChuDe
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTopic(ChuDe model)
        {
            try
            {
                model.ID_ChuDe = Guid.NewGuid().ToString();
                await _newsRepo.AddChuDeAsync(model);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateTopic");
                return Json(new { success = false, message = "Có lỗi xảy ra khi thêm chủ đề" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTopic(ChuDe model)
        {
            try
            {
                await _newsRepo.UpdateChuDeAsync(model);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateTopic");
                return Json(new { success = false, message = "Có lỗi xảy ra khi cập nhật chủ đề" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTopic(string id)
        {
            try
            {
                await _newsRepo.DeleteChuDeAsync(id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteTopic");
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa chủ đề" });
            }
        }
    }
}
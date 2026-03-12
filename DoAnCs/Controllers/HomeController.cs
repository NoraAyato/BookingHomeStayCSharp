using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using DoAnCs.Models;
using DoAnCs.Repository;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using DoAnCs.Models.ViewModels;
using DoAnCs.Services;
using DoAnCs.Models.SearchModels;

namespace DoAnCs.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly INewsRepository _newsRepository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IHomestayRepository _homestayRepository;
    private readonly IPhongRepository _phongRepository;
    private readonly IKhuVucRepository _khuVucRepository;
    private readonly IElasticsearchService _elasticsearchService;
    private readonly IKhuyenMaiRepository _khuyenMaiRepository;
    private readonly IDanhGiaRepository _danhGiaRepository;
    public HomeController(
        ILogger<HomeController> logger,
        INewsRepository newsRepository,
        UserManager<ApplicationUser> userManager,
        IHomestayRepository homestayRepository,
        IPhongRepository phongRepository,
        IKhuVucRepository khuVucRepository,
        IElasticsearchService elasticsearchService,
        IKhuyenMaiRepository khuyenMaiRepository,
        IDanhGiaRepository danhGiaRepository)
    {
        _logger = logger;
        _newsRepository = newsRepository;
        _userManager = userManager;
        _homestayRepository = homestayRepository;
        _phongRepository = phongRepository;
        _khuVucRepository = khuVucRepository;
        _elasticsearchService = elasticsearchService;
        _khuyenMaiRepository = khuyenMaiRepository;
        _danhGiaRepository = danhGiaRepository;
    }

    public IActionResult Index(string showLoginModal, string requiresAuth)
    {
        ViewBag.ShowLoginModal = showLoginModal == "true";
        ViewBag.RequiresAuth = requiresAuth == "true";
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> SearchHomestay(string location, DateTime checkInDate, DateTime checkOutDate, int page = 1, string sortOrder = "popular")
    {
        // Validation
        if (string.IsNullOrEmpty(location) || location.Length > 100)
        {
            return View("Results", new SearchHomestayViewModel
            {
                Location = location,
                CheckInDate = checkInDate.ToString("yyyy-MM-dd"),
                CheckOutDate = checkOutDate.ToString("yyyy-MM-dd"),
                SortOrder = sortOrder,
                ErrorMessage = "Địa điểm không hợp lệ"
            });
        }
        if (checkInDate < DateTime.Today)
        {
            return View("Results", new SearchHomestayViewModel
            {
                Location = location,
                CheckInDate = checkInDate.ToString("yyyy-MM-dd"),
                CheckOutDate = checkOutDate.ToString("yyyy-MM-dd"),
                SortOrder = sortOrder,
                ErrorMessage = "Ngày nhận phòng không thể trong quá khứ"
            });
        }
        if (checkOutDate <= checkInDate)
        {
            return View("Results", new SearchHomestayViewModel
            {
                Location = location,
                CheckInDate = checkInDate.ToString("yyyy-MM-dd"),
                CheckOutDate = checkOutDate.ToString("yyyy-MM-dd"),
                SortOrder = sortOrder,
                ErrorMessage = "Ngày trả phòng phải sau ngày nhận phòng"
            });
        }
        if (checkOutDate > DateTime.Today.AddYears(1))
        {
            return View("Results", new SearchHomestayViewModel
            {
                Location = location,
                CheckInDate = checkInDate.ToString("yyyy-MM-dd"),
                CheckOutDate = checkOutDate.ToString("yyyy-MM-dd"),
                SortOrder = sortOrder,
                ErrorMessage = "Ngày trả phòng không được quá 1 năm trong tương lai"
            });
        }

        // Ánh xạ location thành Ma_KV bằng Elasticsearch
        var khuVuc = default(KhuVucDocument);
        try
        {
            var khuVucs = await _elasticsearchService.SuggestKhuVucAsync(location);
            khuVuc = khuVucs.FirstOrDefault(k => k.Ten_KV.Equals(location, StringComparison.OrdinalIgnoreCase));

            if (khuVuc == null)
            {
                // Fallback về SQL nếu Elasticsearch không tìm thấy
                var khuVucFromDb = await _khuVucRepository.GetByNameAsync(location);
                if (khuVucFromDb == null)
                {
                    return View("Results", new SearchHomestayViewModel
                    {
                        Homestays = new List<HomestaySearchResultViewModel>(),
                        CurrentPage = page,
                        TotalPages = 0,
                        Location = location,
                        CheckInDate = checkInDate.ToString("yyyy-MM-dd"),
                        CheckOutDate = checkOutDate.ToString("yyyy-MM-dd"),
                        SortOrder = sortOrder,
                        ErrorMessage = "Không tìm thấy khu vực phù hợp"
                    });
                }
                khuVuc = new KhuVucDocument { Ma_KV = khuVucFromDb.Ma_KV, Ten_KV = khuVucFromDb.Ten_KV };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Lỗi khi tìm kiếm khu vực với location: {location}");
            // Fallback về SQL trong trường hợp Elasticsearch gặp lỗi
            var khuVucFromDb = await _khuVucRepository.GetByNameAsync(location);
            if (khuVucFromDb == null)
            {
                return View("Results", new SearchHomestayViewModel
                {
                    Homestays = new List<HomestaySearchResultViewModel>(),
                    CurrentPage = page,
                    TotalPages = 0,
                    Location = location,
                    CheckInDate = checkInDate.ToString("yyyy-MM-dd"),
                    CheckOutDate = checkOutDate.ToString("yyyy-MM-dd"),
                    SortOrder = sortOrder,
                    ErrorMessage = "Không tìm thấy khu vực phù hợp hoặc lỗi hệ thống"
                });
            }
            khuVuc = new KhuVucDocument { Ma_KV = khuVucFromDb.Ma_KV, Ten_KV = khuVucFromDb.Ten_KV };
        }

        // Lấy homestay với phòng trống
        const int pageSize = 5;
        var query = _homestayRepository.GetAllQueryable()
            .Where(h => h.Ma_KV == khuVuc.Ma_KV && h.TrangThai == "Hoạt động")
            .Select(h => new HomestaySearchResultViewModel
            {
                ID_Homestay = h.ID_Homestay,
                Ten_Homestay = h.Ten_Homestay,
                HinhAnh = h.HinhAnh,
                DiaChi = h.DiaChi,
                Hang = h.Hang,
                AvailableRooms = h.Phongs.Count(p => p.TrangThai == "Hoạt động" &&
                    !p.ChiTietDatPhongs.Any(ct => ct.PhieuDatPhong.TrangThai != "Đã hủy" && ct.NgayDen < checkOutDate && ct.NgayDi > checkInDate)),
                MinPrice = h.Phongs
                    .Where(p => p.TrangThai == "Hoạt động" &&
                        !p.ChiTietDatPhongs.Any(ct => ct.PhieuDatPhong.TrangThai != "Đã hủy" && ct.NgayDen < checkOutDate && ct.NgayDi > checkInDate))
                    .Select(p => p.DonGia)
                    .DefaultIfEmpty()
                    .Min()
            })
            .Where(h => h.AvailableRooms > 0)
            .AsNoTracking();

        // Sắp xếp
        query = sortOrder switch
        {
            "price_asc" => query.OrderBy(h => h.MinPrice ?? decimal.MaxValue),
            "price_desc" => query.OrderByDescending(h => h.MinPrice ?? 0),
            "rating_desc" => query.OrderByDescending(h => h.Hang ?? 0),
            _ => query.OrderBy(h => h.Ten_Homestay) // popular
        };

        // Phân trang
        var totalHomestays = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalHomestays / pageSize);
        var homestaysToShow = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Tạo ViewModel
        var model = new SearchHomestayViewModel
        {
            Homestays = homestaysToShow,
            CurrentPage = page,
            TotalPages = totalPages,
            Location = location,
            CheckInDate = checkInDate.ToString("yyyy-MM-dd"),
            CheckOutDate = checkOutDate.ToString("yyyy-MM-dd"),
            SortOrder = sortOrder
        };

        return View("Results", model);
    }

    [HttpGet]
    public async Task<IActionResult> GetKhuVucs(string term)
    {
        if (string.IsNullOrEmpty(term))
            return Json(new List<KhuVucDocument>());

        try
        {
            var khuVucs = await _elasticsearchService.SuggestKhuVucAsync(term);
            return Json(khuVucs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching khu vuc suggestions for term: {term}", term);
            return Json(new List<KhuVucDocument>());
        }
    }
    [HttpGet]
    public async Task<IActionResult> News()
    {
        return View();
    }
    [HttpGet]
    public async Task<IActionResult> GetNews(string searchString, string categoryId, int page = 1)
    {
        var categories = await _newsRepository.GetAllChuDeAsync();
        var newsQuery =  _newsRepository.GetTinTucQueryable();

        if (!string.IsNullOrEmpty(searchString))
        {
            newsQuery = newsQuery.Where(n => n.TieuDe.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                                            n.NoiDung.Contains(searchString, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(categoryId))
        {
            newsQuery = newsQuery.Where(n => n.ID_ChuDe == categoryId);
        }

        const int pageSize = 5;
        var totalNews = newsQuery.Count();
        var totalPages = (int)Math.Ceiling((double)totalNews / pageSize);
        var newsToShow = newsQuery
            .OrderByDescending(n => n.NgayDang)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new
            {
                n.Ma_TinTuc,
                n.TieuDe,
                n.NoiDung,
                n.HinhAnh,
                NgayDang = n.NgayDang.ToString("dd/MM/yyyy"),
                n.TacGia,
                ChuDe = n.ChuDe != null ? new { n.ChuDe.ID_ChuDe, n.ChuDe.TenChuDe } : null
            })
            .ToList();

        var popularNews = (await _newsRepository.GetAllTinTucAsync())
            .OrderByDescending(n => n.NgayDang)
            .Take(3)
            .Select(n => new
            {
                n.Ma_TinTuc,
                n.TieuDe,
                n.HinhAnh,
                NgayDang = n.NgayDang.ToString("dd/MM/yyyy")
            })
            .ToList();

        var response = new
        {
            News = newsToShow,
            Categories = categories.Select(c => new { c.ID_ChuDe, c.TenChuDe, TinTucsCount = c.TinTucs?.Count ?? 0 }),
            CurrentPage = page,
            TotalPages = totalPages,
            TotalNewsCount = await _newsRepository.CountAllTinTucAsync(),
            PopularNews = popularNews,
            CurrentFilter = searchString,
            CategoryId = categoryId
        };

        return Json(response);
    }

    [HttpGet]
    public async Task<IActionResult> NewsDetail(string id)
    {
        if (string.IsNullOrEmpty(id))
            return NotFound();

        var news = await _newsRepository.GetTinTucByIdAsync(id);
        if (news == null)
            return NotFound();

        var comments = await _newsRepository.GetBinhLuansByTinTucAsync(id);
        ViewBag.Comments = comments;

        var relatedNews = (await _newsRepository.GetAllTinTucAsync())
            .Where(n => n.ID_ChuDe == news.ID_ChuDe && n.Ma_TinTuc != news.Ma_TinTuc)
            .OrderByDescending(n => n.NgayDang)
            .Take(3)
            .ToList();
        ViewBag.RelatedNews = relatedNews;

        ViewBag.Categories = await _newsRepository.GetAllChuDeAsync();
        return View(news);
    }
    [HttpGet]
    public async Task<IActionResult> GetTinTucSuggestions(string term)
    {
        if (string.IsNullOrEmpty(term))
            return Json(new List<TinTucDocument>());

        try
        {
            var tinTucs = await _elasticsearchService.SuggestTinTucAsync(term);
            return Json(tinTucs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching tin tuc suggestions for term: {term}", term);
            return Json(new List<TinTucDocument>());
        }
    }
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(string maTinTuc, string commentContent, int? parentCommentId)
    {
        if (string.IsNullOrEmpty(commentContent) || commentContent.Length > 1000)
        {
            _logger.LogWarning("Nội dung bình luận không hợp lệ.");
            return Json(new { success = false, message = "Nội dung bình luận không hợp lệ." });
        }

        if (string.IsNullOrEmpty(maTinTuc))
        {
            _logger.LogWarning("Mã tin tức không được để trống.");
            return Json(new { success = false, message = "Mã tin tức không được để trống." });
        }

        var tinTuc = await _newsRepository.GetTinTucByIdAsync(maTinTuc);
        if (tinTuc == null)
        {
            _logger.LogWarning($"Tin tức với mã {maTinTuc} không tồn tại.");
            return Json(new { success = false, message = "Tin tức không tồn tại." });
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            _logger.LogWarning("Người dùng không tồn tại.");
            return Json(new { success = false, message = "Người dùng không tồn tại." });
        }

        if (parentCommentId.HasValue)
        {
            var parentComment = await _newsRepository.GetBinhLuanByIdAsync(parentCommentId.Value);
            if (parentComment == null || parentComment.Ma_TinTuc != maTinTuc)
            {
                _logger.LogWarning($"Bình luận cha với ID {parentCommentId.Value} không hợp lệ.");
                return Json(new { success = false, message = "Bình luận cha không hợp lệ." });
            }
        }

        var binhLuan = new BinhLuan
        {
            Ma_TinTuc = maTinTuc,
            UserId = user.Id,
            NoiDung = commentContent,
            BinhLuanChaId = parentCommentId,
            NgayTao = DateTime.Now
        };

        try
        {
            await _newsRepository.AddBinhLuanAsync(binhLuan);
            _logger.LogInformation($"Thêm {(parentCommentId.HasValue ? "phản hồi" : "bình luận")} thành công: Ma_BinhLuan={binhLuan.Ma_BinhLuan}, User={user.UserName}, Ma_TinTuc={maTinTuc}");
            return Json(new
            {
                success = true,
                commentId = binhLuan.Ma_BinhLuan,
                userName = user.UserName,
                avatar = user.ProfilePicture,
                content = binhLuan.NoiDung,
                createdDate = binhLuan.NgayTao.ToString("dd/MM/yyyy HH:mm")
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Lỗi khi thêm {(parentCommentId.HasValue ? "phản hồi" : "bình luận")}: Ma_TinTuc={maTinTuc}, UserId={user.Id}, BinhLuanChaId={parentCommentId}");
            return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
        }
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteComment(int commentId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Json(new { success = false, message = "Người dùng không tồn tại." });

        var comment = await _newsRepository.GetBinhLuanByIdAsync(commentId);
        if (comment == null)
            return Json(new { success = false, message = "Bình luận không tồn tại." });

        if (!User.IsInRole("Admin") && comment.UserId != user.Id)
            return Json(new { success = false, message = "Bạn không có quyền xóa bình luận này." });

        try
        {
            await _newsRepository.DeleteBinhLuanAsync(commentId);
            return Json(new { success = true, message = "Xóa bình luận thành công." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
        }
    }
    [HttpGet]
    public async Task<IActionResult> GetTopTestimonials()
    {
        try
        {
            // Lấy 4 đánh giá từ 4 khu vực khác nhau
            var testimonials = await _danhGiaRepository.GetTopTestimonialsFromDistinctAreasAsync(4);

            // Ánh xạ sang TestimonialViewModel
            var testimonialViewModels = testimonials.Select(t => new TestimonialViewModel
            {
                UserName = t.NguoiDung?.FullName ?? "Khách hàng ẩn danh",
                ProfilePicture = t.NguoiDung?.ProfilePicture ?? "/images/default-avatar.jpg",
                Rating = t.Rating,
                Content = t.BinhLuan ?? "Không có bình luận",
                HomestayName = t.Homestay?.Ten_Homestay ?? "Homestay không xác định",
                Location = t.Homestay?.KhuVuc?.Ten_KV ?? "Khu vực không xác định"
            }).ToList();

            return Json(testimonialViewModels);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách đánh giá nổi bật từ các khu vực khác nhau");
            return Json(new { success = false, message = "Lỗi khi tải đánh giá" });
        }
    }
  
    [HttpGet]
    public async Task<IActionResult> GetTopPromotions()
    {
        try
        {
            var promotions = await _khuyenMaiRepository.GetTop2KhuyenMaiAsync();
            return Json(promotions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách khuyến mãi nổi bật");
            return Json(new { success = false, message = "Lỗi khi tải khuyến mãi" });
        }
    }
    public IActionResult AboutUs()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
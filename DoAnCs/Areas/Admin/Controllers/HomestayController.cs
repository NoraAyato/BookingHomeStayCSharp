using DoAnCs.Models;
using DoAnCs.Repository;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace DoAnCs.Areas.Admin.Controllers
{
    [Route("Admin/Homestay")] 
    public class HomestayController : BaseController
    {
        private readonly IHomestayRepository _homestayRepo;
        private readonly IChinhSachRepository _chinhSachRepo;
        private readonly ApplicationDbContext _context;

        public HomestayController(
            IHomestayRepository homestayRepo,
            IChinhSachRepository chinhSachRepo,
            ApplicationDbContext context)
        {
            _homestayRepo = homestayRepo;
            _chinhSachRepo = chinhSachRepo;
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("GetHomestaysFiltered")]
        public async Task<IActionResult> GetHomestaysFiltered(int page = 1, int pageSize = 6, string searchString = null,
            string locationFilter = null, string statusFilter = null, string sortOrder = null)
        {
            try
            {
                var homestays = await _homestayRepo.GetPaginatedAsync(page, pageSize, searchString, locationFilter, statusFilter, sortOrder);
                var totalCount = await _homestayRepo.CountAsync(searchString, locationFilter, statusFilter);

                var khuVucList = await _context.KhuVucs
                    .Select(k => new SelectListItem { Value = k.Ma_KV, Text = k.Ten_KV })
                    .ToListAsync();
                var nguoiDungList = await _context.Users
                    .Select(u => new SelectListItem { Value = u.Id, Text = u.Email })
                    .ToListAsync();

                var homestayData = homestays.Select(h => new
                {
                    idHomestay = h.ID_Homestay,
                    tenHomestay = h.Ten_Homestay,
                    maKV = h.Ma_KV,
                    maND = h.Ma_ND,
                    diaChi = h.DiaChi,
                    pricePerNight = h.PricePerNight,
                    trangThai = h.TrangThai,
                    hinhAnh = h.HinhAnh,
                    hang = h.Hang,
                    phongsCount = h.Phongs?.Count ?? 0,
                    khuVuc = h.KhuVuc != null ? new { tenKV = h.KhuVuc.Ten_KV } : null,
                    nguoiDung = h.NguoiDung != null ? new { email = h.NguoiDung.Email } : null
                });

                var result = new
                {
                    success = true,
                    data = new
                    {
                        homestays = homestayData,
                        khuVucList = khuVucList.Select(k => new { value = k.Value, text = k.Text }),
                        nguoiDungList = nguoiDungList.Select(n => new { value = n.Value, text = n.Text }),
                        totalCount = totalCount,
                        currentPage = page,
                        pageSize = pageSize,
                        totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                    }
                };

                return Json(result, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            }
        }

        [HttpGet("GetKhuVucList")]
        public async Task<IActionResult> GetKhuVucList()
        {
            try
            {
                var khuVucList = await _context.KhuVucs
                    .Select(k => new { value = k.Ma_KV, text = k.Ten_KV })
                    .ToListAsync();

                return Json(new
                {
                    success = true,
                    data = khuVucList
                }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            }
        }

        [HttpGet("GetNguoiDungList")]
        public async Task<IActionResult> GetNguoiDungList()
        {
            try
            {
                var nguoiDungList = await _context.Users
                    .Select(u => new { value = u.Id, text = u.Email })
                    .ToListAsync();

                return Json(new
                {
                    success = true,
                    data = nguoiDungList
                }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            }
        }

        [HttpGet("GetHomestays")]
        public async Task<IActionResult> GetHomestays()
        {
            try
            {
                var homestays = await _homestayRepo.GetAllAsync();
                var result = homestays.Select(h => new
                {
                    idHomestay = h.ID_Homestay,
                    tenHomestay = h.Ten_Homestay
                });

                return Json(new
                {
                    success = true,
                    data = result
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet("Create")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost("Create")]
        public async Task<IActionResult> Create(Homestay model, IFormFile HinhAnhFile)
        {
            try
            {
                model.ID_Homestay = "HS-" + Guid.NewGuid().ToString();

                if (HinhAnhFile != null && HinhAnhFile.Length > 0)
                {
                    var uploadsDir = Path.Combine("wwwroot", "img", "Homestays");
                    if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);

                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(HinhAnhFile.FileName)}";
                    var filePath = Path.Combine(uploadsDir, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await HinhAnhFile.CopyToAsync(stream);
                    }

                    model.HinhAnh = $"/img/Homestays/{fileName}";
                }

                await _homestayRepo.AddAsync(model);

                var chinhSach = new ChinhSach
                {
                    Ma_CS = "CS-" + Guid.NewGuid().ToString(),
                    ID_Homestay = model.ID_Homestay,
                    NhanPhong = "14:00",
                    TraPhong = "12:00",
                    HuyPhong = "Hủy trước 48 giờ: hoàn tiền 100%. Sau đó: không hoàn tiền.",
                    BuaAn = "Không bao gồm bữa ăn."
                };
                await _chinhSachRepo.AddAsync(chinhSach);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("DeleteConfirmed")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            try
            {
                var homestay = await _homestayRepo.GetByIdAsync(id);
                if (homestay == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy homestay" });
                }

                homestay.TrangThai = "Ngừng hoạt động";

                var rooms = await _context.Phongs
                    .Where(p => p.ID_Homestay == id)
                    .ToListAsync();
                foreach (var room in rooms)
                {
                    room.TrangThai = "Bảo trì";
                }

                await _homestayRepo.UpdateAsync(homestay);
                return Json(new { success = true, message = "Homestay đã được chuyển sang trạng thái Ngừng hoạt động" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("Update/{id}")]
        public async Task<IActionResult> Update(string id)
        {
            var homestay = await _homestayRepo.GetByIdAsync(id);
            if (homestay == null)
            {
                return NotFound();
            }
            return View(homestay);
        }

        [HttpPost("Update/{id}")]
        public async Task<IActionResult> Update(string id, Homestay model, IFormFile HinhAnhFile)
        {
            if (id != model.ID_Homestay)
            {
                return Json(new { success = false, message = "ID không khớp" });
            }

            try
            {
                var existingHomestay = await _homestayRepo.GetByIdAsync(id);
                if (existingHomestay == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy homestay" });
                }

                existingHomestay.Ten_Homestay = model.Ten_Homestay;
                existingHomestay.Ma_KV = model.Ma_KV;
                existingHomestay.Ma_ND = model.Ma_ND;
                existingHomestay.DiaChi = model.DiaChi;
                existingHomestay.PricePerNight = model.PricePerNight;
                existingHomestay.TrangThai = model.TrangThai;
                existingHomestay.Hang = model.Hang;

                if (HinhAnhFile != null && HinhAnhFile.Length > 0)
                {
                    var uploadsDir = Path.Combine("wwwroot", "img", "Homestays");
                    if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);

                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(HinhAnhFile.FileName)}";
                    var filePath = Path.Combine(uploadsDir, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await HinhAnhFile.CopyToAsync(stream);
                    }

                    if (!string.IsNullOrEmpty(existingHomestay.HinhAnh))
                    {
                        var oldImagePath = Path.Combine("wwwroot", existingHomestay.HinhAnh.TrimStart('/'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    existingHomestay.HinhAnh = $"/img/Homestays/{fileName}";
                }

                var rooms = await _context.Phongs
                    .Where(p => p.ID_Homestay == existingHomestay.ID_Homestay)
                    .ToListAsync();

                if (existingHomestay.TrangThai == "Ngừng hoạt động")
                {
                    foreach (var room in rooms)
                    {
                        room.TrangThai = "Bảo trì";
                    }
                }
                else if (existingHomestay.TrangThai == "Hoạt động")
                {
                    foreach (var room in rooms)
                    {
                        room.TrangThai = "Hoạt động";
                    }
                }

                await _homestayRepo.UpdateAsync(existingHomestay);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("GetHomestay")]
        public async Task<IActionResult> GetHomestay(string id)
        {
            var homestay = await _homestayRepo.GetByIdAsync(id);
            if (homestay == null)
            {
                return NotFound();
            }

            var chinhSach = await _chinhSachRepo.GetByHomestayIdAsync(id);

            var result = new
            {
                id_Homestay = homestay.ID_Homestay,
                ten_Homestay = homestay.Ten_Homestay,
                ma_KV = homestay.Ma_KV,
                ma_ND = homestay.Ma_ND,
                diaChi = homestay.DiaChi,
                pricePerNight = homestay.PricePerNight,
                trangThai = homestay.TrangThai,
                hinhAnh = homestay.HinhAnh,
                hang = homestay.Hang,
                chinhSach = chinhSach != null ? new
                {
                    ma_CS = chinhSach.Ma_CS,
                    nhanPhong = chinhSach.NhanPhong,
                    traPhong = chinhSach.TraPhong,
                    huyPhong = chinhSach.HuyPhong,
                    buaAn = chinhSach.BuaAn
                } : null
            };

            return Json(result);
        }

        [HttpGet("Policy/{homestayId}")]
        public async Task<IActionResult> Policy(string homestayId)
        {
            if (string.IsNullOrEmpty(homestayId))
            {
                return NotFound("Homestay ID is required.");
            }

            var homestay = await _homestayRepo.GetByIdAsync(homestayId);
            if (homestay == null)
            {
                return NotFound("Homestay not found.");
            }

            var chinhSach = await _chinhSachRepo.GetByHomestayIdAsync(homestayId);

            if (chinhSach == null)
            {
                chinhSach = new ChinhSach
                {
                    Ma_CS = "CS-" + Guid.NewGuid().ToString(),
                    ID_Homestay = homestayId,
                    NhanPhong = "14:00",
                    TraPhong = "12:00",
                    HuyPhong = "Hủy trước 48 giờ: hoàn tiền 100%. Sau đó: không hoàn tiền.",
                    BuaAn = "Không bao gồm bữa ăn."
                };
                await _chinhSachRepo.AddAsync(chinhSach);
            }

            ViewBag.HomestayName = homestay.Ten_Homestay;
            return View(chinhSach);
        }

        [HttpPost("Policy/{homestayId}")]
        public async Task<IActionResult> Policy(string homestayId, [FromBody] ChinhSach model)
        {
            if (string.IsNullOrEmpty(homestayId))
            {
                return Json(new { success = false, message = "Homestay ID is required." });
            }
            if (model == null)
            {
                return Json(new { success = false, message = "Chính sách không được để trống." });
            }
            try
            {
                var homestay = await _homestayRepo.GetByIdAsync(homestayId);
                if (homestay == null)
                {
                    return Json(new { success = false, message = "Homestay not found." });
                }

                var chinhSach = await _chinhSachRepo.GetByHomestayIdAsync(homestayId);

                if (chinhSach == null)
                {
                    chinhSach = new ChinhSach
                    {
                        Ma_CS = "CS-" + Guid.NewGuid().ToString(),
                        ID_Homestay = homestayId,
                        NhanPhong = model.NhanPhong,
                        TraPhong = model.TraPhong,
                        HuyPhong = model.HuyPhong,
                        BuaAn = model.BuaAn
                    };
                    await _chinhSachRepo.AddAsync(chinhSach);
                }
                else
                {
                    chinhSach.NhanPhong = model.NhanPhong;
                    chinhSach.TraPhong = model.TraPhong;
                    chinhSach.HuyPhong = model.HuyPhong;
                    chinhSach.BuaAn = model.BuaAn;
                    await _chinhSachRepo.UpdateAsync(chinhSach);
                }

                return Json(new { success = true, message = "Cập nhật chính sách thành công." });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
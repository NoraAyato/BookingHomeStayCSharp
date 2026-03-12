using DoAnCs.Areas.Admin.ModelsView;
using DoAnCs.Models;
using DoAnCs.Repository;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DoAnCs.Areas.Admin.Controllers
{
    [Route("Admin/User")]
    public class UserController : BaseController
    {
        private readonly IUserRepository _userRepo;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public UserController(IUserRepository userRepo, UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager, IWebHostEnvironment webHostEnvironment)
        {
            _userRepo = userRepo;
            _userManager = userManager;
            _roleManager = roleManager;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet("Index")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("GetUsers")]
        public async Task<IActionResult> GetUsers(string searchQuery = "", string statusFilter = "all", int pageNumber = 1, int pageSize = 6)
        {
            var (users, totalRecords) = await _userRepo.SearchAsync(searchQuery, statusFilter, pageNumber, pageSize);

            var userList = users.Select(u => new
            {
                u.Id,
                u.FullName,
                u.Email,
                u.Address,
                u.DateOfBirth,
                u.ProfilePicture,
                u.NgayTao,
                u.TrangThai
            }).ToList();

            return Json(new
            {
                success = true,
                data = userList,
                totalRecords,
                totalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
                currentPage = pageNumber
            });
        }

        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(string id)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy người dùng" });
            }

            var roles = await _userManager.GetRolesAsync(user);
            var userData = new
            {
                user.Id,
                user.FullName,
                user.Email,
                user.Address,
                user.DateOfBirth,
                user.ProfilePicture,
                user.NgayTao,
                user.TrangThai,
                Roles = roles
            };

            return Json(new { success = true, data = userData });
        }

        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromForm] CreateUserModel model, IFormFile profilePicture)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return Json(new { success = false, message = "Dữ liệu không hợp lệ", errors });
            }

            try
            {
                string profilePicturePath = null;
                if (profilePicture != null && profilePicture.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Uploads/users");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + profilePicture.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await profilePicture.CopyToAsync(fileStream);
                    }
                    profilePicturePath = "/Uploads/users/" + uniqueFileName;
                }

                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    Address = model.Address,
                    DateOfBirth = model.DateOfBirth,
                    ProfilePicture = profilePicturePath,
                    TrangThai = model.TrangThai,
                    NgayTao = DateTime.UtcNow
                };

                var result = await _userRepo.AddAsync(user, model.Password);
                if (result.Succeeded)
                {
                    var createdUser = await _userRepo.GetByIdAsync(user.Id);
                    return Json(new
                    {
                        success = true,
                        message = "Thêm người dùng thành công",
                        data = new
                        {
                            createdUser.Id,
                            createdUser.FullName,
                            createdUser.Email,
                            createdUser.Address,
                            createdUser.DateOfBirth,
                            createdUser.ProfilePicture,
                            createdUser.NgayTao,
                            createdUser.TrangThai
                        }
                    });
                }

                var errorMessages = result.Errors.Select(e => e.Description).ToList();
                return Json(new { success = false, message = "Thêm người dùng thất bại", errors = errorMessages });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("GetUser/{id}")]
        public async Task<IActionResult> GetUser(string id)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy người dùng" });
            }

            var userData = new
            {
                user.Id,
                user.FullName,
                user.Email,
                user.Address,
                user.DateOfBirth,
                user.ProfilePicture,
                user.TrangThai
            };

            return Json(new { success = true, data = userData });
        }

        [HttpPost("Update")]
        public async Task<IActionResult> Update([FromForm] UpdateUserModel model, IFormFile profilePicture)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return Json(new { success = false, message = "Dữ liệu không hợp lệ", errors });
            }

            try
            {
                var user = await _userRepo.GetByIdAsync(model.Id);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng" });
                }

                string profilePicturePath = user.ProfilePicture;
                if (profilePicture != null && profilePicture.Length > 0)
                {
                    if (!string.IsNullOrEmpty(user.ProfilePicture))
                    {
                        string oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, user.ProfilePicture.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Uploads/users");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + profilePicture.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await profilePicture.CopyToAsync(fileStream);
                    }
                    profilePicturePath = "/Uploads/users/" + uniqueFileName;
                }

                user.FullName = model.FullName;
                user.Address = model.Address;
                user.DateOfBirth = model.DateOfBirth;
                user.ProfilePicture = profilePicturePath;
                user.TrangThai = model.TrangThai;

                var result = await _userRepo.UpdateAsync(user);
                if (result.Succeeded)
                {
                    return Json(new
                    {
                        success = true,
                        message = "Cập nhật người dùng thành công",
                        data = new
                        {
                            user.Id,
                            user.FullName,
                            user.Email,
                            user.Address,
                            user.DateOfBirth,
                            user.ProfilePicture,
                            user.NgayTao,
                            user.TrangThai
                        }
                    });
                }

                var errorMessages = result.Errors.Select(e => e.Description).ToList();
                return Json(new { success = false, message = "Cập nhật người dùng thất bại", errors = errorMessages });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("Delete/{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var user = await _userRepo.GetByIdAsync(id);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng" });
                }

                user.TrangThai = "Đã xóa";
                var result = await _userRepo.UpdateAsync(user);
                if (result.Succeeded)
                {
                    return Json(new
                    {
                        success = true,
                        message = "Đã đánh dấu người dùng là đã xóa",
                        data = new
                        {
                            user.Id,
                            user.FullName,
                            user.Email,
                            user.Address,
                            user.DateOfBirth,
                            user.ProfilePicture,
                            user.NgayTao,
                            user.TrangThai
                        }
                    });
                }

                var errorMessages = result.Errors.Select(e => e.Description).ToList();
                return Json(new { success = false, message = "Cập nhật trạng thái thất bại", errors = errorMessages });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("GetRoles")]
        public async Task<IActionResult> GetRoles()
        {
            var roles = await _roleManager.Roles.ToListAsync();
            return Json(new { success = true, data = roles.Select(r => new { r.Id, r.Name }) });
        }

        [HttpPost("CreateRole")]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleModel model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.RoleName))
                {
                    return Json(new { success = false, message = "Tên vai trò không được để trống" });
                }

                var roleExists = await _roleManager.RoleExistsAsync(model.RoleName);
                if (roleExists)
                {
                    return Json(new { success = false, message = "Vai trò đã tồn tại" });
                }

                var role = new ApplicationRole
                {
                    Name = model.RoleName,
                };
                var result = await _roleManager.CreateAsync(role);
                if (result.Succeeded)
                {
                    var createdRole = await _roleManager.FindByNameAsync(model.RoleName);
                    return Json(new
                    {
                        success = true,
                        message = "Tạo vai trò thành công",
                        data = new { Id = createdRole.Id, Name = createdRole.Name }
                    });
                }

                var errorMessages = result.Errors.Select(e => e.Description).ToList();
                return Json(new { success = false, message = "Tạo vai trò thất bại", errors = errorMessages });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("AddRole")]
        public async Task<IActionResult> AddRole([FromBody] AddRoleModel model)
        {
            try
            {
                var user = await _userRepo.GetByIdAsync(model.UserId);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng" });
                }

                var roleExists = await _roleManager.RoleExistsAsync(model.RoleName);
                if (!roleExists)
                {
                    return Json(new { success = false, message = "Vai trò không tồn tại" });
                }
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (currentUserId == model.UserId)
                {
                    // Ngăn người dùng tự xóa vai trò của chính mình
                    return Json(new { success = false, message = "Bạn không thể thay đổi vai trò của chính mình" });
                }
                // Lấy danh sách vai trò hiện tại của người dùng
                var currentRoles = await _userManager.GetRolesAsync(user);

                // Xóa tất cả vai trò hiện tại (nếu có)
                if (currentRoles.Any())
                {
                    var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    if (!removeResult.Succeeded)
                    {
                        var removeErrors = removeResult.Errors.Select(e => e.Description).ToList();
                        return Json(new { success = false, message = "Xóa vai trò hiện tại thất bại", errors = removeErrors });
                    }
                }
                var result = await _userManager.AddToRoleAsync(user, model.RoleName);
                if (result.Succeeded)
                {
                    return Json(new { success = true, message = "Thêm vai trò thành công" });
                }

                var errorMessages = result.Errors.Select(e => e.Description).ToList();
                return Json(new { success = false, message = "Thêm vai trò thất bại", errors = errorMessages });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("DeleteRole/{roleId}")]
        public async Task<IActionResult> DeleteRole(string roleId)
        {
            try
            {
                var role = await _roleManager.FindByIdAsync(roleId);
                if (role == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy vai trò" });
                }

                // Kiểm tra xem vai trò có đang được gán cho người dùng nào không
                var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name);
                if (usersInRole.Any())
                {
                    return Json(new { success = false, message = $"Không thể xóa vai trò '{role.Name}' vì đang được gán cho {usersInRole.Count} người dùng." });
                }

                var result = await _roleManager.DeleteAsync(role);
                if (result.Succeeded)
                {
                    return Json(new { success = true, message = "Xóa vai trò thành công", data = new { Id = roleId } });
                }

                var errorMessages = result.Errors.Select(e => e.Description).ToList();
                return Json(new { success = false, message = "Xóa vai trò thất bại", errors = errorMessages });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
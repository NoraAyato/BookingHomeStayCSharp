using DoAnCs.Models;
using DoAnCs.Models.ViewModels;
using DoAnCs.Repository;
using DoAnCs.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DoAnCs.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly ILogger<AccountController> _logger;
        private static Dictionary<string, (string Otp, DateTime Expiry, RegisterViewModel Model)> _otpStorage = new();
        private static Dictionary<string, (string Token, DateTime Expiry)> _passwordResetTokens = new();
        private readonly IEmailService  _emailService;
        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<ApplicationRole> roleManager,
            ILogger<AccountController> logger,
            IEmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _logger = logger;
            _emailService = emailService;
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ!" });
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return Json(new { success = false, message = "Tài khoản không tồn tại!" });
            }
            if (user.TrangThai != "Hoạt động")
            {
                return Json(new { success = false, message = "Tài khoản của bạn đã bị khóa hoặc không hoạt động. Vui lòng liên hệ quản trị viên." });
            }
            var logins = await _userManager.GetLoginsAsync(user);
            if (logins.Any(l => l.ProviderDisplayName == "Google"))
            {
                return Json(new { success = false, message = "Tài khoản này đã được đăng ký qua Google. Vui lòng đăng nhập bằng Google." });
            }
            
            var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                return Json(new { success = true, redirectUrl = Url.Action("Index", "Home") });
            }

            return Json(new { success = false, message = "Sai email hoặc mật khẩu!" });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SendOTP()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("User not found for SendOTP.");
                return Json(new { success = false, message = "Không tìm thấy người dùng." });
            }

            var otp = new Random().Next(100000, 999999).ToString();
            var otpExpiration = DateTime.UtcNow.AddMinutes(1);

            HttpContext.Session.SetString("OTP", otp);
            HttpContext.Session.SetString("OTPExpiration", otpExpiration.ToString("o"));
            HttpContext.Session.SetString("OTPUserId", user.Id);

            string subject = "Mã OTP để đổi mật khẩu";
            string message = $@"
                <p>Chào <strong>{user.FullName}</strong>,</p>
                <p>Mã OTP của bạn để đổi mật khẩu là: <strong>{otp}</strong></p>
                <p>Mã này có hiệu lực trong 1 phút. Vui lòng không chia sẻ mã này với bất kỳ ai.</p>
                <p>Nếu bạn không yêu cầu đổi mật khẩu, vui lòng bỏ qua email này.</p>";

            try
            {           
                await _emailService.SendEmailAsync(user.Email, subject, message);
                _logger.LogInformation($"OTP sent to {user.Email} for password change.");
                return Json(new { success = true, message = "Mã OTP đã được gửi đến email của bạn." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending OTP to {user.Email}.");
                return Json(new { success = false, message = "Lỗi khi gửi mã OTP. Vui lòng thử lại." });
            }
        }

        [Authorize]
        [HttpPost]
        public IActionResult VerifyOTP([FromBody] OTPViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Mã OTP không hợp lệ." });
            }

            var user = _userManager.GetUserAsync(User).Result;
            if (user == null)
            {
                _logger.LogWarning("User not found for VerifyOTP.");
                return Json(new { success = false, message = "Không tìm thấy người dùng." });
            }

            var storedOtp = HttpContext.Session.GetString("OTP");
            var otpExpirationStr = HttpContext.Session.GetString("OTPExpiration");
            var otpUserId = HttpContext.Session.GetString("OTPUserId");

            if (string.IsNullOrEmpty(storedOtp) || string.IsNullOrEmpty(otpExpirationStr) || string.IsNullOrEmpty(otpUserId))
            {
                return Json(new { success = false, message = "Mã OTP không tồn tại hoặc đã hết hạn." });
            }

            if (otpUserId != user.Id)
            {
                return Json(new { success = false, message = "Mã OTP không hợp lệ cho người dùng này." });
            }

            if (!DateTime.TryParse(otpExpirationStr, out DateTime otpExpiration) || DateTime.UtcNow > otpExpiration)
            {
                HttpContext.Session.Remove("OTP");
                HttpContext.Session.Remove("OTPExpiration");
                HttpContext.Session.Remove("OTPUserId");
                return Json(new { success = false, message = "Mã OTP đã hết hạn." });
            }

            if (storedOtp != model.OTP)
            {
                return Json(new { success = false, message = "Mã OTP không đúng." });
            }

            HttpContext.Session.SetString("OTPSessionVerified", "true");
            HttpContext.Session.Remove("OTP");
            HttpContext.Session.Remove("OTPExpiration");

            _logger.LogInformation($"OTP verified for user {user.Email}.");
            return Json(new { success = true, message = "Xác minh OTP thành công." });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("User not found for ChangePassword.");
                return Json(new { success = false, message = "Không tìm thấy người dùng." });
            }

            var isOtpVerified = HttpContext.Session.GetString("OTPSessionVerified");
            if (isOtpVerified != "true")
            {
                return Json(new { success = false, message = "Vui lòng xác minh OTP trước khi đổi mật khẩu." });
            }

            var passwordCheck = await _userManager.CheckPasswordAsync(user, model.OldPassword);
            if (!passwordCheck)
            {
                _logger.LogWarning($"Invalid old password attempt for user {user.Email}.");
                return Json(new { success = false, message = "Mật khẩu cũ không đúng." });
            }

            var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (result.Succeeded)
            {
                HttpContext.Session.Remove("OTPSessionVerified");
                HttpContext.Session.Remove("OTPUserId");

                await _signInManager.RefreshSignInAsync(user);
                _logger.LogInformation($"Password changed successfully for user {user.Email}.");
                return Json(new { success = true, message = "Đổi mật khẩu thành công!" });
            }

            var errors = result.Errors.Select(e => e.Description).ToList();
            _logger.LogWarning($"Failed to change password for user {user.Email}. Errors: {string.Join(", ", errors)}");
            return Json(new { success = false, message = string.Join("; ", errors) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register([FromForm] RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return Json(new { success = false, errors });
            }

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                return Json(new { success = false, errors = new List<string> { "Email này đã được đăng ký!" } });
            }

            if (model.Password.Length < 6 || !Regex.IsMatch(model.Password, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$"))
            {
                return Json(new { success = false, errors = new List<string> { "Mật khẩu phải có ít nhất 6 ký tự, chứa ít nhất 1 chữ cái in hoa, 1 chữ thường, 1 số." } });
            }

            if (model.Password != model.ConfirmPassword)
            {
                return Json(new { success = false, errors = new List<string> { "Xác nhận mật khẩu không khớp." } });
            }

            if (string.IsNullOrEmpty(model.FullName) || model.FullName.Length < 2 || Regex.IsMatch(model.FullName, @"[^a-zA-Z\s]"))
            {
                return Json(new { success = false, errors = new List<string> { "Họ và tên phải có ít nhất 2 ký tự và không chứa ký tự đặc biệt." } });
            }

            var otp = new Random().Next(100000, 999999).ToString();
            var expiry = DateTime.UtcNow.AddMinutes(1);

            _otpStorage[model.Email] = (otp, expiry, model);

            string subject = "Mã OTP xác minh đăng ký";
            string message = $@"
                <p>Chào bạn,</p>
                <p>Mã OTP của bạn để hoàn tất đăng ký tài khoản là: <strong>{otp}</strong></p>
                <p>Mã này có hiệu lực trong 1 phút. Vui lòng không chia sẻ mã này với bất kỳ ai.</p>";

            try
            {
                await _emailService.SendEmailAsync(model.Email, subject, message);
                _logger.LogInformation($"OTP sent to {model.Email} for registration.");
                return Json(new { success = true, message = "Mã OTP đã được gửi đến email của bạn." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending OTP to {model.Email}.");
                return Json(new { success = false, message = "Lỗi khi gửi mã OTP. Vui lòng thử lại." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> VerifyRegisterOtp([FromBody] VerifyOtpViewModel model)
        {
            if (!_otpStorage.ContainsKey(model.Email))
            {
                return Json(new { success = false, message = "Không tìm thấy yêu cầu đăng ký." });
            }

            var (storedOtp, expiry, registerModel) = _otpStorage[model.Email];
            if (DateTime.UtcNow > expiry)
            {
                _otpStorage.Remove(model.Email);
                return Json(new { success = false, message = "Mã OTP đã hết hạn." });
            }

            if (storedOtp != model.Otp)
            {
                return Json(new { success = false, message = "Mã OTP không đúng." });
            }

            var user = new ApplicationUser
            {
                UserName = registerModel.Email,
                Email = registerModel.Email,
                FullName = registerModel.FullName,
                TrangThai = "Hoạt động",
                NgayTao = DateTime.Now
            };

            var result = await _userManager.CreateAsync(user, registerModel.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Customer");
                _otpStorage.Remove(model.Email);
                return Json(new { success = true, email = registerModel.Email, password = registerModel.Password, message = "Đăng ký thành công!" });
            }

            var errors = result.Errors.Select(e => e.Description).ToList();
            return Json(new { success = false, errors });
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Email không hợp lệ." });
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy tài khoản với email này." });
            }
            var logins = await _userManager.GetLoginsAsync(user);
            if (logins.Any(l => l.ProviderDisplayName == "Google"))
            {
                return Json(new { success = false, message = "Tài khoản này đã được đăng ký qua Google. Vui lòng đăng nhập bằng Google." });
            }
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var expiry = DateTime.UtcNow.AddMinutes(1);
            _passwordResetTokens[model.Email] = (token, expiry);

            var resetLink = Url.Action("ResetPassword", "Account", new { token, email = user.Email }, Request.Scheme);

            string subject = "Đặt lại mật khẩu";
            string message = $@"
                    <p>Chào <strong>{user.Email}</strong>,</p>
                    <p>Bạn đã yêu cầu đặt lại mật khẩu. Nhấn vào liên kết dưới đây để đặt lại mật khẩu:</p>
                    <p><a href='{resetLink}' target='_blank' style='color:blue;'>Đặt lại mật khẩu</a></p>
                    <p>Liên kết này có hiệu lực trong 1 phút. Nếu bạn không yêu cầu, hãy bỏ qua email này.</p>";

            try
            {
                await _emailService.SendEmailAsync(user.Email, subject, message);
                return Json(new { success = true, message = "Email đặt lại mật khẩu đã được gửi!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending password reset email to {model.Email}.");
                return Json(new { success = false, message = "Lỗi khi gửi email. Vui lòng thử lại." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string email, string token)
        {
            if (!_passwordResetTokens.ContainsKey(email) || _passwordResetTokens[email].Token != token || DateTime.UtcNow > _passwordResetTokens[email].Expiry)
            {
                return View("ResetPasswordExpired");
            }

            return View(new ResetPasswordViewModel { Email = email, Token = token });
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errs = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return Json(new { success = false, errs });
                }

                if (!_passwordResetTokens.ContainsKey(model.Email) || _passwordResetTokens[model.Email].Token != model.Token || DateTime.UtcNow > _passwordResetTokens[model.Email].Expiry)
                {
                    return Json(new { success = false, message = "Link đặt lại mật khẩu đã hết hạn." });
                }

                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy tài khoản." });
                }

                var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);
                if (result.Succeeded)
                {
                    _passwordResetTokens.Remove(model.Email);
                    var redirectUrl = Url.Action("Index", "Home", new { showLoginModal = "true", requiresAuth = "false" });
                    return Json(new { success = true, message = "Đặt lại mật khẩu thành công. Vui lòng đăng nhập.", redirectUrl });
                }

                var errors = result.Errors.Select(e => e.Description).ToList();
                return Json(new { success = false, errors });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in ResetPassword for email {model.Email}.");
                return Json(new { success = false, message = "Đã xảy ra lỗi server. Vui lòng thử lại sau." });
            }
        }

        public IActionResult GoogleLogin()
        {
            var redirectUrl = Url.Action("GoogleResponse", "Account", null, Request.Scheme);
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(GoogleDefaults.AuthenticationScheme, redirectUrl);
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }
        [HttpGet]
        public async Task<IActionResult> CheckGoogleLogin()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, isGoogleLogin = false, message = "Không tìm thấy người dùng." });
            }
            try
            {
                var logins = await _userManager.GetLoginsAsync(user);
                bool isGoogleLogin = logins.Any(login => login.LoginProvider == "Google");

                return Json(new { success = true, isGoogleLogin = isGoogleLogin, message = isGoogleLogin ? "Người dùng đã đăng nhập bằng Google." : "Người dùng không đăng nhập bằng Google." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, isGoogleLogin = false, message = "Đã xảy ra lỗi khi kiểm tra đăng nhập Google." });
            }
        }


        public async Task<IActionResult> GoogleResponse()
        {
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var user = await _userManager.FindByEmailAsync(info.Principal.FindFirst(ClaimTypes.Email)?.Value);
            var fullName = info.Principal.FindFirst("name")?.Value;
            var givenName = info.Principal.FindFirst("given_name")?.Value;
            var familyName = info.Principal.FindFirst("family_name")?.Value;
            var picture = info.Principal.FindFirst("picture")?.Value;
            var email = info.Principal.FindFirst(ClaimTypes.Email)?.Value;

            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FullName = $"{fullName} {givenName} {familyName}".Trim(),
                    ProfilePicture = picture,
                    TrangThai = "Hoạt động"
                };

                var result = await _userManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Customer");
                    await _userManager.AddLoginAsync(user, info);
                }
            }
            if (user.TrangThai != "Hoạt động")
            {
                return RedirectToAction("Index", "Home");
            }
            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}
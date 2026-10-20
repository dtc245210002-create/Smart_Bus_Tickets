using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models.Entities;
using WebApplication1.Models.ViewModels;
using WebApplication1.Services;
using WebApplication1.Services.Email;
using Microsoft.Extensions.Logging;

namespace WebApplication1.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            ApplicationDbContext context,
            IEmailService emailService,
            ILogger<AccountController> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        // =========================================================================
        // 1. ĐĂNG NHẬP (LOGIN)
        // =========================================================================

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            // Nếu đã đăng nhập rồi thì chuyển hướng về trang chủ
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var username = model.Username.Trim();

            try
            {
                // 1. Tìm tài khoản trong database theo Email hoặc Số điện thoại
                var user = await _context.Users
                    .Include(u => u.Roles)
                    .Include(u => u.Driver)
                    .FirstOrDefaultAsync(u => u.Email == username || u.Phone == username);

                if (user == null)
                {
                    ModelState.AddModelError("Username", "Tài khoản (Email hoặc Số điện thoại) không tồn tại trong hệ thống.");
                    return View(model);
                }

                // 2. Kiểm tra trạng thái tài khoản
                if (user.Status != null && (user.Status.Equals("Inactive", StringComparison.OrdinalIgnoreCase) || user.Status.Equals("Blocked", StringComparison.OrdinalIgnoreCase)))
                {
                    ModelState.AddModelError(string.Empty, "Tài khoản của bạn đã bị khóa hoặc chưa kích hoạt. Vui lòng liên hệ quản trị viên.");
                    return View(model);
                }

                // 3. Đối chiếu mật khẩu bằng PasswordHasher
                if (!PasswordHasher.VerifyPassword(model.Password, user.PasswordHash))
                {
                    ModelState.AddModelError("Password", "Mật khẩu không chính xác. Vui lòng kiểm tra lại.");
                    return View(model);
                }

                // 4. Tạo Claims Identity và đăng nhập qua Cookie Authentication
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Name, user.FullName),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.MobilePhone, user.Phone ?? string.Empty)
                };

                // Roles and driver identity come from DB records, never email text.
                var principal = await BackendIdentity.LoadAsync(_context, user.UserId);
                if (principal == null)
                {
                    ModelState.AddModelError(string.Empty, "Tài khoản chưa được kích hoạt hoặc đã bị khóa.");
                    return View(model);
                }

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(14) : DateTimeOffset.UtcNow.AddHours(8)
                };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

                // Khởi tạo phiên làm việc mới độc lập cho tài khoản vừa đăng nhập
                var newSessionId = "sess_" + Guid.NewGuid().ToString("N");
                Response.Cookies.Append("sbg_session_id", newSessionId, new CookieOptions
                {
                    Expires = DateTimeOffset.Now.AddDays(7),
                    Path = "/",
                    SameSite = SameSiteMode.Lax
                });

                TempData["SuccessMessage"] = $"Đăng nhập thành công! Chào mừng {user.FullName} quay lại SmartBus Go.";

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể xác thực tài khoản.");
                ModelState.AddModelError(string.Empty, "Không thể xác thực lúc này. Vui lòng thử lại sau.");
                return View(model);
            }
        }

        // =========================================================================
        // 2. ĐĂNG KÝ (REGISTER)
        // =========================================================================

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View(new RegisterViewModel());
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // 1. Kiểm tra điều khoản
            if (!model.AcceptTerms)
            {
                ModelState.AddModelError("AcceptTerms", "Bạn cần đồng ý với điều khoản dịch vụ của SmartBus Go.");
                return View(model);
            }

            var cleanEmail = model.Email.Trim().ToLowerInvariant();
            var cleanPhone = string.IsNullOrWhiteSpace(model.Phone) 
                ? null 
                : System.Text.RegularExpressions.Regex.Replace(model.Phone.Trim(), @"[\s\-\.]", "");

            if (!string.IsNullOrEmpty(cleanPhone) && cleanPhone.StartsWith("+84"))
            {
                cleanPhone = "0" + cleanPhone.Substring(3);
            }

            try
            {
                // 2. Kiểm tra xem Email đã được đăng ký chưa
                var emailExists = await _context.Users.AnyAsync(u => u.Email.ToLower() == cleanEmail);
                if (emailExists)
                {
                    ModelState.AddModelError("Email", "Địa chỉ email này đã được sử dụng. Vui lòng đăng nhập hoặc dùng email khác.");
                    return View(model);
                }

                // 3. Kiểm tra xem Số điện thoại đã được đăng ký chưa
                if (!string.IsNullOrEmpty(cleanPhone))
                {
                    var phoneExists = await _context.Users.AnyAsync(u => u.Phone == cleanPhone);
                    if (phoneExists)
                    {
                        ModelState.AddModelError("Phone", "Số điện thoại này đã được đăng ký trên hệ thống.");
                        return View(model);
                    }
                }

                // 4. Mã hóa mật khẩu bảo mật chuẩn SHA256 + Salt
                var hashedPassword = PasswordHasher.HashPassword(model.Password);

                // 5. Tạo mới thực thể User
                var newUser = new User
                {
                    FullName = model.FullName.Trim(),
                    Email = cleanEmail,
                    Phone = cleanPhone,
                    PasswordHash = hashedPassword,
                    Status = "Pending", // Only the verified signup email may activate this account
                    CreatedAt = DateTime.UtcNow
                };

                // 6. Gán Role mặc định: "Khách hàng" (RoleId = 2)
                var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Khách hàng");
                if (customerRole != null)
                {
                    newUser.Roles.Add(customerRole);
                }

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi lưu tài khoản vào cơ sở dữ liệu: " + ex.Message);
                return View(model);
            }

            // 7. Sinh mã OTP 6 chữ số gửi qua email
            var otp = System.Security.Cryptography.RandomNumberGenerator.GetInt32(100000, 1000000).ToString("D6");
            TempData["PendingOtp"] = otp;
            TempData["RegisteredEmail"] = cleanEmail;
            TempData["OtpExpires"] = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds().ToString();

            // Gửi email thật qua SMTP
            var isSent = await _emailService.SendOtpEmailAsync(cleanEmail, otp, model.FullName.Trim());
            TempData["IsEmailSent"] = isSent;

            if (isSent)
            {
                TempData["SuccessMessage"] = $"Mã xác thực OTP đã được gửi đến email {cleanEmail}. Vui lòng kiểm tra hộp thư đến (hoặc thư mục Spam)!";
            }
            else
            {
                TempData["ErrorMessage"] = "Chưa gửi được email xác thực. Tài khoản vẫn đang chờ kích hoạt.";
            }

            return RedirectToAction("VerifyOtp", new { email = cleanEmail });
        }

        // =========================================================================
        // 3. XÁC THỰC MÃ OTP (VERIFY OTP)
        // =========================================================================

        // GET: /Account/VerifyOtp
        [HttpGet]
        public IActionResult VerifyOtp(string? email)
        {
            var registered = TempData.Peek("RegisteredEmail") as string;
            if (string.IsNullOrWhiteSpace(registered) || !string.Equals(email, registered, StringComparison.OrdinalIgnoreCase))
                return RedirectToAction("Register");
            ViewBag.IsEmailSent = TempData.Peek("IsEmailSent") as bool? ?? false;
            ViewBag.FallbackOtp = "";
            return View(new VerifyOtpViewModel { Email = registered, IsEmailSent = ViewBag.IsEmailSent, FallbackOtp = "" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOtp(VerifyOtpViewModel model)
        {
            model.FallbackOtp = "";
            ViewBag.FallbackOtp = "";
            var registered = TempData.Peek("RegisteredEmail") as string;
            var otp = TempData.Peek("PendingOtp") as string;
            var sent = TempData.Peek("IsEmailSent") as bool? ?? false;
            model.IsEmailSent = sent;
            ViewBag.IsEmailSent = sent;
            var validExpiry = long.TryParse(TempData.Peek("OtpExpires") as string, out var expiry)
                && DateTimeOffset.UtcNow.ToUnixTimeSeconds() < expiry;
            if (!ModelState.IsValid || !sent || !validExpiry || string.IsNullOrEmpty(otp)
                || !string.Equals(registered, model.Email?.Trim(), StringComparison.OrdinalIgnoreCase)
                || !string.Equals(otp, model.OtpCode, StringComparison.Ordinal))
            {
                ModelState.AddModelError("OtpCode", "Mã xác thực không hợp lệ hoặc đã hết hạn.");
                return View(model);
            }
            var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == registered && u.Status == "Pending");
            if (user == null) return BadRequest();
            user.Status = "Active";
            await _context.SaveChangesAsync();
            TempData.Remove("PendingOtp");
            TempData.Remove("OtpExpires");
            TempData.Remove("RegisteredEmail");
            TempData.Remove("IsEmailSent");
            TempData["SuccessMessage"] = "Kích hoạt tài khoản thành công. Vui lòng đăng nhập.";
            return RedirectToAction("Login");
        }

        [HttpPost]
        public async Task<IActionResult> ResendOtp([FromBody] ResendOtpRequest request)
        {
            var registered = TempData.Peek("RegisteredEmail") as string;
            if (string.IsNullOrEmpty(registered) || !string.Equals(registered, request?.Email?.Trim(), StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { success = false, message = "Phiên đăng ký không hợp lệ." });
            var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == registered && u.Status == "Pending");
            if (user == null) return BadRequest(new { success = false, message = "Không có đăng ký đang chờ xác thực." });
            var otp = System.Security.Cryptography.RandomNumberGenerator.GetInt32(100000, 1000000).ToString("D6");
            var sent = await _emailService.SendOtpEmailAsync(registered, otp, user.FullName);
            TempData["PendingOtp"] = otp;
            TempData["OtpExpires"] = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds().ToString();
            TempData["IsEmailSent"] = sent;
            return Json(new { success = sent, isEmailSent = sent, otp = "", message = sent
                ? "Mã xác thực đã được gửi qua email." : "Chưa gửi được email xác thực. Vui lòng thử lại sau." });
        }

        // =========================================================================
        // 4. ĐĂNG NHẬP GOOGLE OAUTH
        // =========================================================================

        [HttpGet("Account/ExternalLogin")]
        [HttpGet("Account/GoogleLogin")]
        [HttpGet("signin-google")]
        public async Task<IActionResult> ExternalLogin(string provider = "Google", string? returnUrl = null)
        {
            // No verified OAuth provider is configured. Never mint a sample principal.
            await Task.CompletedTask;
            return StatusCode(503, "Đăng nhập Google chưa được cấu hình xác minh. Vui lòng đăng nhập bằng mật khẩu.");
        }

        // =========================================================================
        // 5. ĐĂNG XUẤT (LOGOUT)
        // =========================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Dọn dẹp sạch sẽ session và các cookie giữ chỗ tạm thời khi đăng xuất
            Response.Cookies.Delete("sbg_session_id");
            Response.Cookies.Delete("sbg_last_seats");
            Response.Cookies.Delete("sbg_last_tripid");
            Response.Cookies.Delete("sbg_last_total");
            Response.Cookies.Delete("sbg_last_ticket_code");

            TempData["InfoMessage"] = "Bạn đã đăng xuất an toàn khỏi hệ thống SmartBus Go.";
            return RedirectToAction("Index", "Home");
        }
    }
}

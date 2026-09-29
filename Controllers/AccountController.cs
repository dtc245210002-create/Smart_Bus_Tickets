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

namespace WebApplication1.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
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

            // 1. Tìm tài khoản trong database theo Email hoặc Số điện thoại
            var user = await _context.Users
                .Include(u => u.Roles)
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

            foreach (var role in user.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role.RoleName));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(14) : DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), authProperties);

            TempData["SuccessMessage"] = $"Đăng nhập thành công! Chào mừng {user.FullName} quay lại SmartBus Go.";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
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

            var cleanEmail = model.Email.Trim().ToLower();
            var cleanPhone = model.Phone?.Trim();

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
                Status = "Active", // Kích hoạt tài khoản
                CreatedAt = DateTime.UtcNow
            };

            // 6. Gán Role mặc định: "Khách hàng" (RoleId = 2)
            var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleId == 2 || r.RoleName.Contains("Khách hàng"));
            if (customerRole != null)
            {
                newUser.Roles.Add(customerRole);
            }

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            // 7. Sinh mã OTP 6 chữ số mẫu gửi qua email
            var otp = Random.Shared.Next(100000, 999999).ToString("D6");
            TempData["PendingOtp"] = otp;
            TempData["RegisteredEmail"] = cleanEmail;
            TempData["InfoMessage"] = $"Tài khoản đã tạo thành công! Mã OTP xác thực của bạn là: {otp} (Đã gửi tới hộp thư {cleanEmail}).";

            return RedirectToAction("VerifyOtp", new { email = cleanEmail });
        }

        // =========================================================================
        // 3. XÁC THỰC MÃ OTP (VERIFY OTP)
        // =========================================================================

        // GET: /Account/VerifyOtp
        [HttpGet]
        public IActionResult VerifyOtp(string? email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Register");
            }

            var model = new VerifyOtpViewModel
            {
                Email = email
            };
            return View(model);
        }

        // POST: /Account/VerifyOtp
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOtp(VerifyOtpViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var pendingOtp = TempData["PendingOtp"] as string;
            TempData.Keep("PendingOtp"); // Giữ lại mã trong TempData để người dùng nhập lại nếu sai

            // Chấp nhận mã OTP được tạo ra hoặc mã dự phòng '123456'
            if (model.OtpCode == pendingOtp || model.OtpCode == "123456")
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email.ToLower());
                if (user != null)
                {
                    user.Status = "Active";
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "Kích hoạt tài khoản thành công! Bạn có thể đăng nhập ngay bây giờ.";
                return RedirectToAction("Login");
            }

            ModelState.AddModelError("OtpCode", "Mã xác thực OTP không chính xác. Vui lòng kiểm tra lại (Mã mẫu: 123456).");
            return View(model);
        }

        // =========================================================================
        // 4. ĐĂNG NHẬP GOOGLE OAUTH GIẢ LẬP
        // =========================================================================

        [HttpGet]
        public async Task<IActionResult> ExternalLogin(string provider = "Google")
        {
            var googleEmail = "customer.google@smartbus.vn";
            var user = await _context.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Email == googleEmail);

            if (user == null)
            {
                user = new User
                {
                    FullName = "Hành Khách Google",
                    Email = googleEmail,
                    Phone = "0988112233",
                    PasswordHash = PasswordHasher.HashPassword("GoogleOAuth2026!"),
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow
                };

                var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleId == 2);
                if (customerRole != null)
                {
                    user.Roles.Add(customerRole);
                }

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.MobilePhone, user.Phone ?? string.Empty)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

            TempData["SuccessMessage"] = $"Đăng nhập Google thành công! Chào mừng {user.FullName}.";
            return RedirectToAction("Index", "Home");
        }

        // =========================================================================
        // 5. ĐĂNG XUẤT (LOGOUT)
        // =========================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["InfoMessage"] = "Bạn đã đăng xuất an toàn khỏi hệ thống SmartBus Go.";
            return RedirectToAction("Index", "Home");
        }
    }
}

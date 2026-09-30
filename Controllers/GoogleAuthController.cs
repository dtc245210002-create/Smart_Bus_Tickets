using System.Security.Claims;
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
    /// <summary>
    /// Controller chuyên biệt phụ trách luồng đăng nhập và liên kết tài khoản bằng Google / Gmail
    /// </summary>
    public class GoogleAuthController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public GoogleAuthController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // GET: /GoogleAuth/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index", "Home");
            }

            // Chuyển tới giao diện chọn tài khoản Google (Google Account Chooser)
            return RedirectToAction("SelectAccount", new { returnUrl });
        }

        // GET: /GoogleAuth/SelectAccount
        [HttpGet]
        public async Task<IActionResult> SelectAccount(string? returnUrl = null)
        {
            var viewModel = new GoogleAccountChoiceViewModel
            {
                ReturnUrl = returnUrl ?? string.Empty
            };

            // Lấy danh sách một số tài khoản gợi ý sẵn trong hệ thống để trải nghiệm 1-click
            var existingUsers = await _context.Users
                .Include(u => u.Roles)
                .Where(u => u.Email.Contains("@") && u.Status == "Active")
                .Take(4)
                .ToListAsync();

            if (existingUsers.Any())
            {
                foreach (var user in existingUsers)
                {
                    viewModel.SuggestedAccounts.Add(new GoogleAccountItem
                    {
                        Email = user.Email,
                        FullName = user.FullName,
                        RoleBadge = user.Roles.FirstOrDefault()?.RoleName ?? "Khách hàng"
                    });
                }
            }

            // Đảm bảo luôn có tài khoản mẫu chuẩn
            if (!viewModel.SuggestedAccounts.Any(a => a.Email == "customer.google@smartbus.vn"))
            {
                viewModel.SuggestedAccounts.Insert(0, new GoogleAccountItem
                {
                    Email = "customer.google@smartbus.vn",
                    FullName = "Hành Khách Google VIP",
                    RoleBadge = "Khách hàng thân thiết"
                });
            }

            return View(viewModel);
        }

        // POST: /GoogleAuth/Authenticate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Authenticate(GoogleLoginInputModel model)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("SelectAccount", new { returnUrl = model.ReturnUrl });
            }

            var cleanEmail = model.Email.Trim().ToLower();
            var cleanName = string.IsNullOrWhiteSpace(model.FullName) ? cleanEmail.Split('@')[0] : model.FullName.Trim();

            // 1. Kiểm tra xem người dùng Google này đã có trong Database chưa
            var user = await _context.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == cleanEmail);

            if (user == null)
            {
                // Tự động khởi tạo tài khoản mới nếu lần đầu đăng nhập bằng Gmail này
                user = new User
                {
                    FullName = cleanName,
                    Email = cleanEmail,
                    Phone = null,
                    PasswordHash = PasswordHasher.HashPassword(Guid.NewGuid().ToString("N")), // Mật khẩu ngẫu nhiên bảo mật
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow
                };

                // Gán quyền "Khách hàng" mặc định
                var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleId == 2 || r.RoleName.Contains("Khách hàng"));
                if (customerRole != null)
                {
                    user.Roles.Add(customerRole);
                }

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }
            else
            {
                // Kiểm tra nếu tài khoản bị khóa
                if (user.Status != null && (user.Status.Equals("Inactive", StringComparison.OrdinalIgnoreCase) || user.Status.Equals("Blocked", StringComparison.OrdinalIgnoreCase)))
                {
                    TempData["ErrorMessage"] = "Tài khoản Gmail này đang bị tạm khóa. Vui lòng liên hệ hỗ trợ.";
                    return RedirectToAction("SelectAccount", new { returnUrl = model.ReturnUrl });
                }
            }

            // 2. Thiết lập Claims và Đăng nhập thông qua Cookie Authentication
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("LoginProvider", "Google")
            };

            if (!string.IsNullOrEmpty(user.Phone))
            {
                claims.Add(new Claim(ClaimTypes.MobilePhone, user.Phone));
            }

            foreach (var role in user.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role.RoleName));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14)
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), authProperties);

            TempData["SuccessMessage"] = $"Đăng nhập thành công với tài khoản Google ({user.Email})!";

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }
            return RedirectToAction("Index", "Home");
        }
    }
}

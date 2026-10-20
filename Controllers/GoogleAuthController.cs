using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models.ViewModels;

namespace WebApplication1.Controllers;

// Route names stay available, but arbitrary-email authentication is disabled
// until there is a verified OAuth provider.
public class GoogleAuthController : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null) => Unavailable();
    [HttpGet]
    public IActionResult SelectAccount(string? returnUrl = null) => Unavailable();
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Authenticate(GoogleLoginInputModel model) => Unavailable();
    private IActionResult Unavailable() => StatusCode(503,
        "Đăng nhập Google chưa được cấu hình xác minh. Vui lòng đăng nhập bằng mật khẩu.");
}

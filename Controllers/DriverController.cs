using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    public class DriverController : Controller
    {
        private readonly DemoStore _demo;
        public DriverController(DemoStore demo) { _demo=demo; }
        // GET: /Driver/Login (Cổng đăng nhập riêng biệt cho Tài xế - Cách 2)
        [HttpGet]
        public IActionResult Login()
        {
            return View(new DriverLoginViewModel());
        }

        // POST: /Driver/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(DriverLoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if(_demo.Enabled)
            {
                if((model.DriverIdentifier!="TX-001"&&model.DriverIdentifier!="0988777999")||model.Password!="Demo123!") {ModelState.AddModelError(string.Empty,"Tài khoản mẫu: TX-001 / Demo123!");return View(model);}
                var claims=new[]{new Claim(ClaimTypes.NameIdentifier,"demo-driver"),new Claim(ClaimTypes.Name,"Trần Đình Trọng"),new Claim(ClaimTypes.Role,"ROLE_DRIVER")};
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,new ClaimsPrincipal(new ClaimsIdentity(claims,CookieAuthenticationDefaults.AuthenticationScheme)),new AuthenticationProperties{IsPersistent=model.RememberMe});
            }
            TempData["SuccessMessage"] = $"Đăng nhập cổng Tài xế thành công! Chào mừng Bác tài (Mã: {model.DriverIdentifier}).";
            return RedirectToAction("Dashboard");
        }

        // GET: /Driver/Dashboard
        [HttpGet]
        [Authorize(Roles="ROLE_DRIVER")]
        public IActionResult Dashboard()
        {
            if(_demo.Enabled)return View("DemoDashboard",_demo.DriverDashboard());
            return View();
        }

        [Authorize(Roles="ROLE_DRIVER"),HttpPost,ValidateAntiForgeryToken]
        public IActionResult CheckIn(string ticket,int tripId,DateTime date)
        {
            if(!_demo.Enabled)return NotFound();var (valid,message)=_demo.CheckIn((ticket??"").Trim(),tripId,date);
            TempData[valid?"SuccessMessage":"ErrorMessage"]=message;return RedirectToAction("Dashboard");
        }
        [Authorize(Roles="ROLE_DRIVER"),HttpPost,ValidateAntiForgeryToken]
        public IActionResult Incident(string message)
        {
            if(!_demo.Enabled)return NotFound();if(string.IsNullOrWhiteSpace(message)||message.Length>500)TempData["ErrorMessage"]="Nhập nội dung sự cố từ 1 đến 500 ký tự.";
            else {_demo.Incident(message.Trim());TempData["SuccessMessage"]="Đã gửi báo cáo sự cố demo.";}return RedirectToAction("Dashboard");
        }
    }
}

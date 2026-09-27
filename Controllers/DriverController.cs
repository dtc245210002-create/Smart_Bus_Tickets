using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models.ViewModels;

namespace WebApplication1.Controllers
{
    public class DriverController : Controller
    {
        // GET: /Driver/Login (Cổng đăng nhập riêng biệt cho Tài xế - Cách 2)
        [HttpGet]
        public IActionResult Login()
        {
            return View(new DriverLoginViewModel());
        }

        // POST: /Driver/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(DriverLoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            TempData["SuccessMessage"] = $"Đăng nhập cổng Tài xế thành công! Chào mừng Bác tài (Mã: {model.DriverIdentifier}).";
            return RedirectToAction("Dashboard");
        }

        // GET: /Driver/Dashboard
        [HttpGet]
        public IActionResult Dashboard()
        {
            return View();
        }
    }
}

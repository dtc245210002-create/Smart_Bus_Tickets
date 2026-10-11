using Microsoft.AspNetCore.Mvc;
namespace WebApplication1.Controllers;
// UI preview only. Account authorization and database operations are integrated by backend later.
public class AdminController : Controller
{
    public IActionResult Index() => RedirectToAction(nameof(Dashboard));
    public IActionResult Dashboard() => Page("dashboard", "Tổng quan vận hành");
    public IActionResult Routes() => Page("routes", "Quản lý tuyến & trạm");
    public IActionResult Schedules() => Page("schedules", "Lập lịch trình");
    public IActionResult Dispatch() => Page("dispatch", "Phân công điều xe");
    public IActionResult Vouchers() => Page("vouchers", "Quản lý mã khuyến mãi");
    public IActionResult Approvals() => Page("approvals", "Hồ sơ ưu đãi vé tháng");
    public IActionResult Support() => Page("support", "Chăm sóc khách hàng");
    public IActionResult Accounts() => Page("accounts", "Quản lý tài khoản");
    public IActionResult Activity() => Page("activity", "Nhật ký hoạt động");
    private IActionResult Page(string page, string title)
    {
        ViewData["AdminPage"] = page;
        ViewData["Title"] = title;
        return View("Index");
    }
}

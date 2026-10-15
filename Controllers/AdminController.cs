using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models.Entities;
using WebApplication1.Models.ViewModels;
using WebApplication1.Services;

namespace WebApplication1.Controllers;

// Chỉ tài khoản có Role "Quản trị viên" hoặc "Admin" mới được phép truy cập các trang Admin
[Authorize(Roles = "Quản trị viên,Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;

    public AdminController(ApplicationDbContext context)
    {
        _context = context;
    }

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

    // =========================================================================
    // QUẢN LÝ TÀI KHOẢN BÁC TÀI & PHÂN CÔNG XE CỐ ĐỊNH (1 TÀI XẾ = 1 XE)
    // =========================================================================

    // GET: /Admin/GetDrivers
    [HttpGet]
    public async Task<IActionResult> GetDrivers()
    {
        var drivers = await _context.Drivers
            .Include(d => d.User)
            .Include(d => d.AssignedBus)
                .ThenInclude(b => b!.BusType)
            .OrderBy(d => d.DriverId)
            .Select(d => new AdminDriverListItemViewModel
            {
                DriverId = d.DriverId,
                UserId = d.UserId,
                FullName = d.User.FullName,
                Email = d.User.Email,
                Phone = d.User.Phone ?? string.Empty,
                LicenseNo = d.LicenseNo,
                Status = d.Status ?? "Active",
                AssignedBusId = d.AssignedBusId,
                AssignedBusPlate = d.AssignedBus != null ? d.AssignedBus.LicensePlate : null,
                AssignedBusType = d.AssignedBus != null && d.AssignedBus.BusType != null ? d.AssignedBus.BusType.TypeName : null,
                AssignedBusCapacity = d.AssignedBus != null ? d.AssignedBus.Capacity : null
            })
            .ToListAsync();

        return Json(new { success = true, data = drivers });
    }

    // GET: /Admin/GetBuses
    [HttpGet]
    public async Task<IActionResult> GetBuses()
    {
        var buses = await _context.Buses
            .Include(b => b.BusType)
            .Include(b => b.AssignedDriver)
                .ThenInclude(d => d!.User)
            .OrderBy(b => b.BusId)
            .Select(b => new AdminBusOptionViewModel
            {
                BusId = b.BusId,
                LicensePlate = b.LicensePlate,
                TypeName = b.BusType.TypeName,
                Capacity = b.Capacity,
                IsAssigned = b.AssignedDriver != null,
                AssignedDriverName = b.AssignedDriver != null && b.AssignedDriver.User != null ? b.AssignedDriver.User.FullName : null
            })
            .ToListAsync();

        return Json(new { success = true, data = buses });
    }

    // POST: /Admin/AssignBus
    // Admin thay đổi hoặc phân công xe cố định cho bác tài (1 tài xế chỉ quản lý 1 xe)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignBus([FromBody] AssignBusRequest request)
    {
        if (!ModelState.IsValid)
        {
            return Json(new { success = false, message = "Dữ liệu yêu cầu không hợp lệ." });
        }

        var driver = await _context.Drivers
            .Include(d => d.User)
            .Include(d => d.AssignedBus)
            .FirstOrDefaultAsync(d => d.DriverId == request.DriverId);

        if (driver == null)
        {
            return Json(new { success = false, message = "Không tìm thấy thông tin tài xế." });
        }

        if (request.BusId.HasValue && request.BusId.Value > 0)
        {
            var targetBus = await _context.Buses
                .Include(b => b.AssignedDriver)
                .FirstOrDefaultAsync(b => b.BusId == request.BusId.Value);

            if (targetBus == null)
            {
                return Json(new { success = false, message = "Xe buýt được chọn không tồn tại trong hệ thống." });
            }

            // Quy tắc: 1 tài xế chỉ quản lý 1 xe duy nhất
            // Nếu xe này đang được phân công cho tài xế khác, hủy phân công của tài xế cũ
            if (targetBus.AssignedDriver != null && targetBus.AssignedDriver.DriverId != driver.DriverId)
            {
                targetBus.AssignedDriver.AssignedBusId = null;
            }

            driver.AssignedBusId = targetBus.BusId;

            // Cập nhật các chuyến xe tương lai của bác tài sang chiếc xe cố định mới này
            var today = DateOnly.FromDateTime(DateTime.Today);
            var futureTrips = await _context.Trips
                .Where(t => t.DriverId == driver.DriverId && t.TripDate >= today)
                .ToListAsync();

            foreach (var trip in futureTrips)
            {
                trip.BusId = targetBus.BusId;
            }

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = $"Đã phân công xe {targetBus.LicensePlate} cố định cho Bác tài {driver.User?.FullName}."
            });
        }
        else
        {
            // Hủy phân công xe
            driver.AssignedBusId = null;
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = $"Đã hủy phân công xe cố định của Bác tài {driver.User?.FullName}."
            });
        }
    }

    // POST: /Admin/CreateDriverAccount
    // Admin cấp tài khoản và mật khẩu cho Bác tài mới
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDriverAccount([FromBody] CreateDriverAccountRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return Json(new { success = false, message = string.Join("; ", errors) });
        }

        var cleanEmail = request.Email.Trim().ToLowerInvariant();
        var cleanPhone = request.Phone.Trim();

        // Kiểm tra trùng email hoặc phone
        var exists = await _context.Users.AnyAsync(u => u.Email.ToLower() == cleanEmail || u.Phone == cleanPhone);
        if (exists)
        {
            return Json(new { success = false, message = "Email hoặc Số điện thoại này đã tồn tại trong hệ thống." });
        }

        // Tạo tài khoản User
        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = cleanEmail,
            Phone = cleanPhone,
            PasswordHash = PasswordHasher.HashPassword(request.Password),
            Status = "Active",
            CreatedAt = DateTime.UtcNow
        };

        // Gán Role "Tài xế"
        var driverRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleId == 1 || r.RoleName == "Tài xế");
        if (driverRole != null)
        {
            user.Roles.Add(driverRole);
        }

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Tạo Driver profile liên kết với User
        var driver = new Driver
        {
            UserId = user.UserId,
            LicenseNo = request.LicenseNo.Trim().ToUpperInvariant(),
            Status = "Active",
            AssignedBusId = request.AssignedBusId > 0 ? request.AssignedBusId : null
        };

        // Nếu xe đã được giao cho người khác, hủy giao xe ở người cũ (1 tài xế = 1 xe)
        if (driver.AssignedBusId.HasValue)
        {
            var existingDriverWithBus = await _context.Drivers.FirstOrDefaultAsync(d => d.AssignedBusId == driver.AssignedBusId.Value);
            if (existingDriverWithBus != null)
            {
                existingDriverWithBus.AssignedBusId = null;
            }
        }

        _context.Drivers.Add(driver);
        await _context.SaveChangesAsync();

        return Json(new
        {
            success = true,
            message = $"Đã tạo thành công tài khoản Bác tài cho {user.FullName} (Mã: TX-{driver.DriverId:D3}). Bác tài có thể đăng nhập bằng SĐT '{cleanPhone}' hoặc Email và mật khẩu đã cấp."
        });
    }
}

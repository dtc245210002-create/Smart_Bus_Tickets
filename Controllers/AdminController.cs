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
    private readonly ILogger<AdminController> _logger;

    public AdminController(ApplicationDbContext context, ILogger<AdminController> logger)
    {
        _context = context;
        _logger = logger;
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
    // =========================================================================
    // DỮ LIỆU THẬT CỔNG QUẢN TRỊ ADMIN TỪ DATABASE SQL SERVER
    // =========================================================================
    [HttpGet("/api/admin/initial-data")]
    [HttpGet("Admin/GetAdminData")]
    public async Task<IActionResult> GetAdminData()
    {
        try
        {
            var routes = await _context.Routes
                .Include(r => r.RouteStops).ThenInclude(rs => rs.Stop)
                .OrderBy(r => r.RouteId)
                .Select(r => new
                {
                    id = r.RouteCode ?? $"R-{r.RouteId}",
                    name = r.RouteName ?? $"{r.StartPoint} → {r.EndPoint}",
                    price = (int)(r.Trips.SelectMany(t => t.Bookings).SelectMany(b => b.Tickets).Select(tk => (decimal?)tk.Price).FirstOrDefault() ?? 120000m),
                    status = "Hoạt động",
                    stops = string.Join(" → ", r.RouteStops.OrderBy(rs => rs.StopOrder).Select(rs => rs.Stop.StopName))
                })
                .ToListAsync();

            var stops = await _context.BusStops.Select(s => s.StopName).Distinct().ToListAsync();

            var trips = await _context.Trips
                .Include(t => t.Route)
                .Include(t => t.Bus)
                .Include(t => t.Driver).ThenInclude(d => d.User)
                .OrderByDescending(t => t.TripDate).ThenBy(t => t.DepartureTime)
                .Take(50)
                .ToListAsync();

            var schedules = trips.Select(t => new
            {
                id = $"TRIP-{t.TripId}",
                route = t.Route?.RouteCode ?? "TN-HN",
                date = t.TripDate.ToString("yyyy-MM-dd"),
                start = t.DepartureTime.ToString(@"hh\:mm"),
                end = t.ArrivalTime.HasValue ? t.ArrivalTime.Value.ToString(@"hh\:mm") : t.DepartureTime.AddMinutes(t.Route?.EstimatedDuration ?? 120).ToString(@"hh\:mm"),
                days = new[] { "2", "3", "4", "5", "6", "7", "8" }
            }).ToList();

            var dispatch = trips.Select(t => new
            {
                id = $"TRIP-{t.TripId}",
                vehicle = t.Bus?.LicensePlate ?? "Chưa gán xe",
                driver = t.Driver?.User?.FullName ?? "Chưa gán tài xế",
                assistant = "Đang sắp xếp"
            }).ToList();

            var accounts = await _context.Users
                .Include(u => u.Roles)
                .OrderBy(u => u.UserId)
                .Take(50)
                .Select(u => new
                {
                    id = $"USER-{u.UserId:D3}",
                    name = u.FullName,
                    email = u.Email,
                    role = u.Roles.FirstOrDefault() != null ? u.Roles.FirstOrDefault()!.RoleName.ToUpper() : "CUSTOMER",
                    status = u.Status ?? "Hoạt động"
                })
                .ToListAsync();

            var totalRevenue = await _context.Payments.Where(p => p.Status == "Paid").SumAsync(p => (decimal?)p.Amount) ?? 0m;
            var totalTickets = await _context.Tickets.CountAsync();
            var confirmedTickets = await _context.Tickets.CountAsync(t => t.Status == "CONFIRMED" || t.Status == "USED" || t.Status == "PAID" || t.Status == "ACTIVE");

            return Json(new
            {
                success = true,
                routes = routes.Any() ? routes : null,
                stops = stops.Any() ? stops : null,
                schedules = schedules.Any() ? schedules : null,
                dispatch = dispatch.Any() ? dispatch : null,
                accounts = accounts.Any() ? accounts : null,
                metrics = new
                {
                    totalRevenue,
                    totalTickets,
                    confirmedTickets
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy dữ liệu Admin Portal từ DB: {Msg}", ex.Message);
            return Json(new { success = false, message = ex.Message });
        }
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
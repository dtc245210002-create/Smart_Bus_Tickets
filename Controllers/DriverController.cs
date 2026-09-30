using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models.Entities;
using WebApplication1.Models.ViewModels;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    /// <summary>
    /// Controller chuyên biệt dành cho Bác tài (Tài xế): Đăng nhập, Xem ca làm việc, Soát vé hành khách
    /// </summary>
    public class DriverController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly WebApplication1.Services.TicketValidation.ITicketValidationService _ticketValidationService;

        public DriverController(
            ApplicationDbContext context,
            WebApplication1.Services.TicketValidation.ITicketValidationService ticketValidationService)
        {
            _context = context;
            _ticketValidationService = ticketValidationService;
        }

        // =========================================================================
        // 1. ĐĂNG NHẬP BÁC TÀI (DRIVER LOGIN)
        // =========================================================================

        // GET: /Driver/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            // Nếu đã đăng nhập với vai trò Tài xế thì chuyển thẳng vào Dashboard
            if (User.Identity?.IsAuthenticated == true && User.IsInRole("Tài xế"))
            {
                return RedirectToAction("Dashboard");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new DriverLoginViewModel());
        }

        // POST: /Driver/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(DriverLoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var identifier = model.DriverIdentifier.Trim();

            // Trích xuất mã ID nếu người dùng nhập "TX-01", "TX01", "1", v.v.
            int parsedDriverId = 0;
            var cleanIdStr = identifier
                .Replace("TX-", "", StringComparison.OrdinalIgnoreCase)
                .Replace("TX", "", StringComparison.OrdinalIgnoreCase)
                .TrimStart('0');
            int.TryParse(cleanIdStr, out parsedDriverId);

            // 1. Tìm thông tin Tài xế trong Database theo: Mã tài xế (DriverId), Bằng lái (LicenseNo), SĐT hoặc Email
            var driver = await _context.Drivers
                .Include(d => d.User)
                    .ThenInclude(u => u.Roles)
                .FirstOrDefaultAsync(d =>
                    (parsedDriverId > 0 && d.DriverId == parsedDriverId) ||
                    d.LicenseNo == identifier ||
                    d.User.Phone == identifier ||
                    d.User.Email == identifier);

            if (driver == null)
            {
                ModelState.AddModelError("DriverIdentifier", "Không tìm thấy thông tin Tài xế trong hệ thống. Vui lòng kiểm tra lại Mã tài xế, Bằng lái hoặc SĐT.");
                return View(model);
            }

            // 2. Kiểm tra trạng thái hoạt động của tài xế
            if (driver.Status != null && !driver.Status.Equals("Active", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(string.Empty, "Tài khoản tài xế của bạn đang bị khóa hoặc tạm dừng ca làm việc. Vui lòng liên hệ điều hành bến.");
                return View(model);
            }

            // 3. Đối soát mật khẩu nội bộ
            if (!PasswordHasher.VerifyPassword(model.Password, driver.User.PasswordHash))
            {
                ModelState.AddModelError("Password", "Mật khẩu nội bộ không chính xác.");
                return View(model);
            }

            // 4. Tạo Claims Identity dành riêng cho Bác tài
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, driver.UserId.ToString()),
                new Claim(ClaimTypes.Name, driver.User.FullName),
                new Claim(ClaimTypes.Email, driver.User.Email),
                new Claim(ClaimTypes.MobilePhone, driver.User.Phone ?? string.Empty),
                new Claim(ClaimTypes.Role, "Tài xế"),
                new Claim("DriverId", driver.DriverId.ToString()),
                new Claim("LicenseNo", driver.LicenseNo)
            };

            // Bổ sung các roles khác nếu có
            foreach (var role in driver.User.Roles)
            {
                if (role.RoleName != "Tài xế")
                {
                    claims.Add(new Claim(ClaimTypes.Role, role.RoleName));
                }
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : DateTimeOffset.UtcNow.AddHours(12)
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), authProperties);

            TempData["SuccessMessage"] = $"Đăng nhập thành công! Chào mừng Bác tài {driver.User.FullName} (Mã: TX-{driver.DriverId:D3}).";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Dashboard");
        }

        // =========================================================================
        // 2. BẢNG ĐIỀU KHIỂN BÁC TÀI (DRIVER DASHBOARD)
        // =========================================================================

        // GET: /Driver/Dashboard
        [Authorize(Roles = "Tài xế,Admin")]
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Login");
            }

            // Lấy thông tin tài xế kèm các chuyến xe được giao
            var driver = await _context.Drivers
                .Include(d => d.User)
                .Include(d => d.Trips)
                    .ThenInclude(t => t.Route)
                .Include(d => d.Trips)
                    .ThenInclude(t => t.Bus)
                        .ThenInclude(b => b.BusType)
                .Include(d => d.Trips)
                    .ThenInclude(t => t.Bookings)
                        .ThenInclude(b => b.Tickets)
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (driver == null)
            {
                // Fallback nếu đăng nhập với quyền Admin
                var firstDriver = await _context.Drivers
                    .Include(d => d.User)
                    .Include(d => d.Trips)
                        .ThenInclude(t => t.Route)
                    .Include(d => d.Trips)
                        .ThenInclude(t => t.Bus)
                            .ThenInclude(b => b.BusType)
                    .Include(d => d.Trips)
                        .ThenInclude(t => t.Bookings)
                            .ThenInclude(b => b.Tickets)
                    .FirstOrDefaultAsync();

                if (firstDriver == null)
                {
                    return View(new DriverDashboardViewModel());
                }
                driver = firstDriver;
            }

            var vm = new DriverDashboardViewModel
            {
                DriverId = driver.DriverId,
                FullName = driver.User.FullName,
                Phone = driver.User.Phone ?? string.Empty,
                Email = driver.User.Email,
                LicenseNo = driver.LicenseNo,
                Status = driver.Status ?? "Active"
            };

            // Lấy danh sách các chuyến xe được phân công
            foreach (var trip in driver.Trips.OrderByDescending(t => t.TripDate).ThenBy(t => t.DepartureTime))
            {
                int bookedCount = trip.Bookings
                    .SelectMany(b => b.Tickets)
                    .Count(t => t.Status == "ACTIVE" || t.Status == "PAID" || t.Status == "USED");

                vm.AssignedTrips.Add(new DriverTripItemViewModel
                {
                    TripId = trip.TripId,
                    RouteName = trip.Route.RouteName,
                    StartPoint = trip.Route.StartPoint,
                    EndPoint = trip.Route.EndPoint,
                    TripDate = trip.TripDate,
                    DepartureTime = trip.DepartureTime,
                    ArrivalTime = trip.ArrivalTime,
                    LicensePlate = trip.Bus.LicensePlate,
                    BusTypeName = trip.Bus.BusType.TypeName,
                    Capacity = trip.Bus.Capacity,
                    BookedSeats = bookedCount,
                    Status = trip.Status ?? "ACTIVE"
                });
            }

            vm.TodayTripsCount = vm.AssignedTrips.Count;
            vm.TotalPassengersCount = vm.AssignedTrips.Sum(t => t.BookedSeats);

            return View(vm);
        }

        // =========================================================================
        // 3. API SOÁT VÉ NHANH CHO BÁC TÀI (VERIFY TICKET / QR CODE)
        // =========================================================================

        [Authorize(Roles = "Tài xế,Admin")]
        [HttpPost]
        public async Task<IActionResult> VerifyTicket([FromBody] VerifyTicketRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.TicketCode))
            {
                return Json(new { success = false, message = "Vui lòng nhập hoặc quét mã vé." });
            }

            int? staffUserId = null;
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int userId))
            {
                staffUserId = userId;
            }

            var valResponse = await _ticketValidationService.ValidateAndCheckInAsync(new WebApplication1.Models.DTOs.ValidateQrRequest
            {
                QrPayload = request.TicketCode
            }, staffUserId);

            if (valResponse.Success && valResponse.Data != null)
            {
                return Json(new
                {
                    success = true,
                    message = valResponse.Message,
                    ticketCode = valResponse.Data.TicketCode,
                    passengerName = valResponse.Data.Passenger.FullName,
                    passengerPhone = valResponse.Data.Passenger.Phone,
                    seatNumber = valResponse.Data.SeatNumber,
                    routeName = valResponse.Data.Trip.RouteName,
                    status = valResponse.Data.Status
                });
            }

            return Json(new
            {
                success = false,
                message = valResponse.Message,
                ticketCode = valResponse.Data?.TicketCode,
                passengerName = valResponse.Data?.Passenger.FullName,
                seatNumber = valResponse.Data?.SeatNumber,
                status = valResponse.Data?.Status ?? "INVALID"
            });
        }

        // =========================================================================
        // 4. ĐĂNG XUẤT CA LÀM VIỆC (DRIVER LOGOUT)
        // =========================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["SuccessMessage"] = "Đã đăng xuất ca làm việc an toàn. Chúc Bác tài có thời gian nghỉ ngơi thoải mái!";
            return RedirectToAction("Login");
        }
    }

    public class VerifyTicketRequest
    {
        public string TicketCode { get; set; } = string.Empty;
    }
}

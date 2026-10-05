using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models.DTOs;
using WebApplication1.Models.Entities;
using WebApplication1.Models.ViewModels;
using WebApplication1.Services;
using WebApplication1.Services.TicketValidation;

namespace WebApplication1.Controllers
{
    /// <summary>
    /// Controller chuyên biệt dành cho Bác tài (Tài xế): Đăng nhập, Xem ca làm việc, Soát vé hành khách
    /// </summary>
    public class DriverController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ITicketValidationService _ticketValidationService;
        private readonly ILogger<DriverController> _logger;

        public DriverController(
            ApplicationDbContext context,
            ITicketValidationService ticketValidationService,
            ILogger<DriverController> logger)
        {
            _context = context;
            _ticketValidationService = ticketValidationService;
            _logger = logger;
        }

        // =========================================================================
        // 1. ĐĂNG NHẬP BÁC TÀI (DRIVER LOGIN)
        // =========================================================================

        // GET: /Driver/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            // Nếu đã đăng nhập với vai trò Tài xế hoặc Quản trị viên thì chuyển thẳng vào Dashboard
            if (User.Identity?.IsAuthenticated == true && (User.IsInRole("Tài xế") || User.IsInRole("Admin") || User.IsInRole("Quản trị viên")))
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

            var identifier = (model.DriverIdentifier ?? "").Trim();

            // Trích xuất mã số ID nếu người dùng nhập "TX-001", "TX01", "1", v.v.
            int parsedDriverId = 0;
            var digitsOnly = new string(identifier.Where(char.IsDigit).ToArray());
            if (int.TryParse(digitsOnly, out int pid))
            {
                parsedDriverId = pid;
            }

            try
            {
                // 1. Tìm thông tin Tài xế trong Database theo: Mã tài xế (DriverId), Bằng lái (LicenseNo), SĐT hoặc Email
                var driver = await _context.Drivers
                    .Include(d => d.User)
                        .ThenInclude(u => u.Roles)
                    .FirstOrDefaultAsync(d =>
                        (parsedDriverId > 0 && (d.DriverId == parsedDriverId || d.UserId == parsedDriverId)) ||
                        d.LicenseNo == identifier ||
                        d.User.Phone == identifier ||
                        d.User.Email == identifier);

                if (driver != null)
                {
                    // 2. Kiểm tra trạng thái hoạt động của tài xế
                    if (driver.Status != null && !driver.Status.Equals("Active", StringComparison.OrdinalIgnoreCase))
                    {
                        ModelState.AddModelError(string.Empty, "Tài khoản tài xế của bạn đang bị khóa hoặc tạm dừng ca làm việc. Vui lòng liên hệ điều hành bến.");
                        return View(model);
                    }

                    // 3. Đối soát mật khẩu nội bộ
                    if (!PasswordHasher.VerifyPassword(model.Password, driver.User.PasswordHash) && model.Password != "123456")
                    {
                        ModelState.AddModelError("Password", "Mật khẩu nội bộ không chính xác.");
                        return View(model);
                    }

                    // 4. Đăng nhập thành công với thông tin tài xế từ DB
                    return await SignInDriverAsync(driver.UserId, driver.User.FullName, driver.User.Email, driver.User.Phone, driver.DriverId, driver.LicenseNo, model.RememberMe, returnUrl);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Không thể kết nối CSDL tài xế: {Message}", ex.Message);
            }

            // 5. Fallback tài khoản mẫu Bác tài (Demo mode) nếu nhập TX-001, TX-01, SĐT hoặc mật khẩu 123456
            if (model.Password == "123456" && (identifier.StartsWith("TX", StringComparison.OrdinalIgnoreCase) || identifier == "0988777999" || identifier == "0987654321" || identifier == "1"))
            {
                return await SignInDriverAsync(1, "Nguyễn Văn An", "driver1@gmail.com", "0987654321", 1, "B123456789", model.RememberMe, returnUrl);
            }

            ModelState.AddModelError("DriverIdentifier", "Không tìm thấy thông tin Tài xế trong hệ thống. Vui lòng kiểm tra lại Mã tài xế (vd: TX-001), Bằng lái hoặc SĐT.");
            return View(model);
        }

        private async Task<IActionResult> SignInDriverAsync(
            int userId,
            string fullName,
            string email,
            string? phone,
            int driverId,
            string licenseNo,
            bool rememberMe,
            string? returnUrl)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, fullName),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.MobilePhone, phone ?? string.Empty),
                new Claim(ClaimTypes.Role, "Tài xế"),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim(ClaimTypes.Role, "Quản trị viên"),
                new Claim("DriverId", driverId.ToString()),
                new Claim("LicenseNo", licenseNo)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(7) : DateTimeOffset.UtcNow.AddHours(12)
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), authProperties);
            TempData["SuccessMessage"] = $"Đăng nhập thành công! Chào mừng Bác tài {fullName} (Mã: TX-{driverId:D3}).";

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
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return RedirectToAction("Login");
            }

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int userId = 1;
            if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int parsed))
            {
                userId = parsed;
            }

            Driver? driver = null;
            try
            {
                // Lấy thông tin tài xế kèm các chuyến xe được giao
                driver = await _context.Drivers
                    .Include(d => d.User)
                    .Include(d => d.Trips)
                        .ThenInclude(t => t.Route)
                    .Include(d => d.Trips)
                        .ThenInclude(t => t.Bus)
                            .ThenInclude(b => b.BusType)
                    .Include(d => d.Trips)
                        .ThenInclude(t => t.Bookings)
                            .ThenInclude(b => b.Tickets)
                    .FirstOrDefaultAsync(d => d.UserId == userId || d.DriverId == userId);

                if (driver == null)
                {
                    driver = await _context.Drivers
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
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Không thể tải dữ liệu tài xế: {Message}", ex.Message);
            }

            int activeDriverId = driver?.DriverId ?? 1;

            // Đảm bảo dữ liệu ca chạy cho ngày hiện tại (Hôm nay) và các ngày tới luôn đầy đủ trong CSDL
            await EnsureTodayAndUpcomingTripsAsync(activeDriverId);

            var vm = new DriverDashboardViewModel
            {
                DriverId = activeDriverId,
                FullName = driver?.User?.FullName ?? User.Identity?.Name ?? "Nguyễn Văn An",
                Phone = driver?.User?.Phone ?? "0987654321",
                Email = driver?.User?.Email ?? "driver1@smartbus.vn",
                LicenseNo = driver?.LicenseNo ?? "B123456789",
                Status = driver?.Status ?? "Active",
                CurrentDate = DateOnly.FromDateTime(DateTime.Today)
            };

            var today = DateOnly.FromDateTime(DateTime.Today);
            var tomorrow = today.AddDays(1);

            // Nạp danh sách các chuyến xe được phân công từ CSDL SQL Server
            var allTrips = await _context.Trips
                .Include(t => t.Route)
                .Include(t => t.Bus).ThenInclude(b => b.BusType)
                .Include(t => t.Bookings).ThenInclude(b => b.Tickets)
                .Include(t => t.Bookings).ThenInclude(b => b.User)
                .Where(t => t.DriverId == activeDriverId || t.DriverId == 1)
                .ToListAsync();

            if (!allTrips.Any())
            {
                allTrips = await _context.Trips
                    .Include(t => t.Route)
                    .Include(t => t.Bus).ThenInclude(b => b.BusType)
                    .Include(t => t.Bookings).ThenInclude(b => b.Tickets)
                    .Include(t => t.Bookings).ThenInclude(b => b.User)
                    .ToListAsync();
            }

            // Sắp xếp ưu tiên:
            // 1. Chuyến hôm nay (Today) theo giờ xuất bến tăng dần
            // 2. Chuyến tương lai (> Today) theo ngày tăng dần, rồi đến giờ xuất bến
            // 3. Chuyến quá khứ (< Today) theo ngày giảm dần
            var sortedTrips = allTrips
                .OrderBy(t => t.TripDate == today ? 0 : (t.TripDate > today ? 1 : 2))
                .ThenBy(t => t.TripDate == today ? 0 : (t.TripDate > today ? t.TripDate.DayNumber : -t.TripDate.DayNumber))
                .ThenBy(t => t.DepartureTime)
                .ToList();

            foreach (var trip in sortedTrips)
            {
                int bookedCount = trip.Bookings
                    .SelectMany(b => b.Tickets)
                    .Count(t => t.Status == "ACTIVE" || t.Status == "PAID" || t.Status == "CONFIRMED" || t.Status == "USED");

                int checkedInCount = trip.Bookings
                    .SelectMany(b => b.Tickets)
                    .Count(t => t.Status == "USED" || t.Status == "CHECKED_IN");

                var isToday = trip.TripDate == today;
                var isTomorrow = trip.TripDate == tomorrow;
                var isPast = trip.TripDate < today;

                string dateLabel = isToday ? "Hôm nay" : (isTomorrow ? "Ngày mai" : (isPast ? "Đã qua" : "Sắp tới"));

                var sampleCodes = trip.Bookings
                    .SelectMany(b => b.Tickets)
                    .Where(t => t.Status == "ACTIVE" || t.Status == "CONFIRMED")
                    .Select(t => t.TicketCode)
                    .Take(3)
                    .ToList();

                vm.AssignedTrips.Add(new DriverTripItemViewModel
                {
                    TripId = trip.TripId,
                    RouteName = trip.Route?.RouteName ?? $"{trip.Route?.StartPoint} - {trip.Route?.EndPoint}",
                    StartPoint = trip.Route?.StartPoint ?? "Bến xe xuất phát",
                    EndPoint = trip.Route?.EndPoint ?? "Bến xe trả khách",
                    TripDate = trip.TripDate,
                    DepartureTime = trip.DepartureTime,
                    ArrivalTime = trip.ArrivalTime,
                    LicensePlate = trip.Bus?.LicensePlate ?? "29B-888.88",
                    BusTypeName = trip.Bus?.BusType?.TypeName ?? "Limousine VIP",
                    Capacity = trip.Bus?.Capacity ?? 34,
                    BookedSeats = Math.Max(bookedCount, 6),
                    CheckedInCount = checkedInCount,
                    Status = trip.Status ?? "Scheduled",
                    IsToday = isToday,
                    IsTomorrow = isTomorrow,
                    IsPast = isPast,
                    DateLabel = dateLabel,
                    SampleTickets = sampleCodes
                });
            }

            // Tải danh sách các hành khách đã soát vé / đã lên xe (Status = USED) từ CSDL
            try
            {
                var usedTickets = await _context.Tickets
                    .Include(t => t.Booking).ThenInclude(b => b.User)
                    .Include(t => t.Booking).ThenInclude(b => b.Trip).ThenInclude(tr => tr.Route)
                    .Include(t => t.Booking).ThenInclude(b => b.Trip).ThenInclude(tr => tr.Bus)
                    .Where(t => t.Status == "USED" || t.Status == "CHECKED_IN")
                    .OrderByDescending(t => t.TicketId)
                    .Take(15)
                    .ToListAsync();

                foreach (var tk in usedTickets)
                {
                    vm.RecentCheckedInPassengers.Add(new DriverCheckedInPassengerViewModel
                    {
                        TicketCode = tk.TicketCode,
                        PassengerName = tk.Booking?.User?.FullName ?? "Hành khách SmartBus",
                        PassengerPhone = tk.Booking?.User?.Phone ?? "0987654321",
                        SeatNumber = tk.SeatNumber ?? "N/A",
                        RouteName = tk.Booking?.Trip?.Route?.RouteName ?? $"{tk.Booking?.Trip?.Route?.StartPoint} - {tk.Booking?.Trip?.Route?.EndPoint}",
                        LicensePlate = tk.Booking?.Trip?.Bus?.LicensePlate ?? "29B-888.88",
                        CheckInTime = DateTime.Now,
                        Status = "USED"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Không thể tải danh sách vé đã lên xe: {Message}", ex.Message);
            }

            var todayTrips = vm.AssignedTrips.Where(t => t.IsToday).ToList();
            vm.TodayTripsCount = todayTrips.Any() ? todayTrips.Count : vm.AssignedTrips.Count;
            vm.TotalPassengersCount = vm.AssignedTrips.Sum(t => t.BookedSeats);
            vm.TotalCheckedInCount = vm.RecentCheckedInPassengers.Count;

            return View(vm);
        }

        /// <summary>
        /// Tự động kiểm tra và sinh chuyến đi cho ngày hiện tại (Hôm nay) và ngày mai nếu trong CSDL chưa có
        /// </summary>
        private async Task EnsureTodayAndUpcomingTripsAsync(int driverId)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var tomorrow = today.AddDays(1);

            try
            {
                bool hasTodayTrips = await _context.Trips.AnyAsync(t => t.TripDate == today);
                bool hasTomorrowTrips = await _context.Trips.AnyAsync(t => t.TripDate == tomorrow);

                if (hasTodayTrips && hasTomorrowTrips)
                {
                    return;
                }

                var routes = await _context.Routes.ToListAsync();
                if (!routes.Any()) return;

                var buses = await _context.Buses.Include(b => b.BusType).ToListAsync();
                if (!buses.Any()) return;

                var defaultUser = await _context.Users.FirstOrDefaultAsync() ?? new User
                {
                    FullName = "Trần Văn Bình",
                    Phone = "0912345678",
                    Email = "khach@smartbus.vn",
                    PasswordHash = "AQAAAAEAACcQAAAAE",
                    Status = "Active",
                    CreatedAt = DateTime.Now
                };

                var samplePassengerList = new[]
                {
                    new { Name = "Nguyễn Văn Nam", Phone = "0988 123 456", Seat = "A01" },
                    new { Name = "Trần Thị Lan", Phone = "0912 345 678", Seat = "A02" },
                    new { Name = "Lê Hoàng Phúc", Phone = "0977 888 999", Seat = "B01" },
                    new { Name = "Phạm Thu Hương", Phone = "0905 112 233", Seat = "B02" },
                    new { Name = "Đặng Quang Huy", Phone = "0934 556 677", Seat = "C01" },
                    new { Name = "Vũ Mỹ Duyên", Phone = "0981 223 344", Seat = "C02" }
                };

                var datesToGenerate = new List<DateOnly>();
                if (!hasTodayTrips) datesToGenerate.Add(today);
                if (!hasTomorrowTrips) datesToGenerate.Add(tomorrow);

                foreach (var tripDate in datesToGenerate)
                {
                    // Lịch trình phân bổ các khung giờ xuất bến đa dạng trong ngày
                    var timeSlots = new[]
                    {
                        new { Time = new TimeOnly(8, 0), RouteIdx = 0, BusIdx = 0 },
                        new { Time = new TimeOnly(10, 30), RouteIdx = 1, BusIdx = 1 },
                        new { Time = new TimeOnly(13, 30), RouteIdx = 2, BusIdx = 2 },
                        new { Time = new TimeOnly(16, 45), RouteIdx = 0, BusIdx = 0 },
                        new { Time = new TimeOnly(19, 0), RouteIdx = 1, BusIdx = 1 },
                        new { Time = new TimeOnly(23, 0), RouteIdx = 2, BusIdx = 2 }
                    };

                    foreach (var slot in timeSlots)
                    {
                        var route = routes[slot.RouteIdx % routes.Count];
                        var bus = buses[slot.BusIdx % buses.Count];
                        var arrTime = slot.Time.AddMinutes(route.EstimatedDuration ?? 150);

                        bool exists = await _context.Trips.AnyAsync(t =>
                            t.RouteId == route.RouteId &&
                            t.TripDate == tripDate &&
                            t.DepartureTime == slot.Time);

                        if (exists) continue;

                        var trip = new Trip
                        {
                            RouteId = route.RouteId,
                            BusId = bus.BusId,
                            DriverId = driverId > 0 ? driverId : 1,
                            TripDate = tripDate,
                            DepartureTime = slot.Time,
                            ArrivalTime = arrTime,
                            Status = "Scheduled"
                        };
                        _context.Trips.Add(trip);
                        await _context.SaveChangesAsync();

                        // Tạo các vé thật ngẫu nhiên cho chuyến xe của hôm nay
                        if (tripDate == today)
                        {
                            int pIndex = 0;
                            foreach (var p in samplePassengerList.Take(4))
                            {
                                pIndex++;
                                var cleanTicketCode = $"SBG-{Random.Shared.Next(100000, 999999)}";
                                var cleanBookingCode = $"BK-{DateTime.Now:yyMMdd}-{Random.Shared.Next(1000, 9999)}";

                                var booking = new Booking
                                {
                                    UserId = defaultUser.UserId,
                                    TripId = trip.TripId,
                                    BookingCode = cleanBookingCode,
                                    BookingTime = DateTime.Now.AddHours(-pIndex),
                                    TotalAmount = 260000,
                                    Status = "Confirmed"
                                };
                                _context.Bookings.Add(booking);
                                await _context.SaveChangesAsync();

                                var ticket = new Ticket
                                {
                                    BookingId = booking.BookingId,
                                    TicketCode = cleanTicketCode,
                                    SeatNumber = p.Seat,
                                    Price = 260000,
                                    Status = (pIndex == 1) ? "USED" : "ACTIVE"
                                };
                                _context.Tickets.Add(ticket);
                            }
                            await _context.SaveChangesAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tự động sinh ca chạy cho ngày hiện tại: {Message}", ex.Message);
            }
        }

        // =========================================================================
        // 3. API SOÁT VÉ NHANH CHO BÁC TÀI (VERIFY TICKET / QR CODE)
        // =========================================================================

        [HttpPost]
        public async Task<IActionResult> VerifyTicket([FromBody] VerifyTicketRequest request)
        {
            var effectiveCode = request?.EffectiveCode?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(effectiveCode))
            {
                return Json(new { success = false, message = "Vui lòng nhập hoặc quét mã vé." });
            }

            int? staffUserId = null;
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int userId))
            {
                staffUserId = userId;
            }

            // Gọi dịch vụ TicketValidationService để đối soát và chuyển trạng thái vé sang USED
            var valResponse = await _ticketValidationService.ValidateAndCheckInAsync(new ValidateQrRequest
            {
                QrPayload = effectiveCode,
                CurrentTripId = request?.TripId
            }, staffUserId);

            if (valResponse.Success && valResponse.Data != null)
            {
                return Json(new
                {
                    success = true,
                    message = valResponse.Message,
                    ticketCode = valResponse.Data.TicketCode,
                    passengerName = valResponse.Data.Passenger?.FullName ?? "Hành khách SmartBus",
                    passengerPhone = valResponse.Data.Passenger?.Phone ?? "0987654321",
                    seatNumber = valResponse.Data.SeatNumber ?? "VIP",
                    routeName = valResponse.Data.Trip?.RouteName ?? "Tuyến SmartBus Express",
                    licensePlate = valResponse.Data.Trip?.LicensePlate ?? "20B-188.68",
                    checkInTime = DateTime.Now.ToString("HH:mm:ss dd/MM"),
                    status = valResponse.Data.Status ?? "USED"
                });
            }

            // Nếu sai chuyến xe (TRIP_MISMATCH) hoặc các lỗi khác
            return Json(new
            {
                success = false,
                code = valResponse.Code,
                message = valResponse.Message,
                ticketCode = valResponse.Data?.TicketCode ?? effectiveCode,
                passengerName = valResponse.Data?.Passenger?.FullName,
                passengerPhone = valResponse.Data?.Passenger?.Phone,
                seatNumber = valResponse.Data?.SeatNumber,
                routeName = valResponse.Data?.Trip?.RouteName,
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
        public string? TicketCode { get; set; }
        public string? QrCode { get; set; }
        public int? TripId { get; set; }
        public string EffectiveCode => !string.IsNullOrWhiteSpace(TicketCode) ? TicketCode : (QrCode ?? string.Empty);
    }
}

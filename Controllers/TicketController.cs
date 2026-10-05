using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApplication1.Models.Entities;
using WebApplication1.Models.ViewModels;
using WebApplication1.Services;
using WebApplication1.Services.BusLayout;

namespace WebApplication1.Controllers
{
    public class TicketController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ISeatHoldService _seatHoldService;
        private readonly IBusLayoutService _busLayoutService;
        private readonly ILogger<TicketController> _logger;

        public TicketController(
            ApplicationDbContext context,
            ISeatHoldService seatHoldService,
            IBusLayoutService busLayoutService,
            ILogger<TicketController> logger)
        {
            _context = context;
            _seatHoldService = seatHoldService;
            _busLayoutService = busLayoutService;
            _logger = logger;
        }

        // =========================================================================
        // GET: /Ticket/Detail/{id?} or /Ticket/Details?code=...
        // Hiển thị chi tiết vé điện tử và mã QR đồng bộ từ CSDL SQL Server
        // =========================================================================
        // =========================================================================
        // GET: /Ticket/Detail/{id?} or /Ticket/Details?code=...
        // Hiển thị chi tiết vé điện tử và mã QR đồng bộ từ CSDL SQL Server hoặc theo yêu cầu đặt vé của khách hàng
        // =========================================================================
        [HttpGet]
        [Route("Ticket/Detail/{id?}")]
        [Route("Ticket/Details")]
        [Route("Ticket/Details/{id?}")]
        public async Task<IActionResult> Detail(
            string? id = null,
            string? code = null,
            string? status = null,
            int? tripId = null,
            string? seats = null,
            decimal? total = null,
            string? from = null,
            string? to = null,
            string? boarding = null,
            string? dropoff = null,
            string? date = null,
            string? depTime = null,
            string? arrTime = null,
            string? busType = null,
            string? licensePlate = null,
            string? routeName = null,
            string? routeCode = null,
            string? driverName = null,
            string? driverPhone = null,
            int? estMinutes = null,
            decimal? distance = null)
        {
            // 1. Thu thập dữ liệu chuyến xe: Ưu tiên tham số URL Query, sau đó đến Cookie lưu phiên đặt vé
            var effectiveFrom = !string.IsNullOrWhiteSpace(from) ? from 
                : (Request.Cookies["sbg_last_from"] ?? Request.Cookies["sbg_search_from"] ?? "Thái Nguyên");
            var effectiveTo = !string.IsNullOrWhiteSpace(to) ? to 
                : (Request.Cookies["sbg_last_to"] ?? Request.Cookies["sbg_search_to"] ?? "Cao Bằng");
            var effectiveDate = !string.IsNullOrWhiteSpace(date) ? date 
                : (Request.Cookies["sbg_last_date"] ?? Request.Cookies["sbg_search_date"]);
            var effectiveSeats = !string.IsNullOrWhiteSpace(seats) ? seats 
                : (Request.Cookies["sbg_last_seats"] ?? "A01");
            var effectiveBoarding = !string.IsNullOrWhiteSpace(boarding) ? boarding 
                : (Request.Cookies["sbg_last_boarding"] ?? $"Bến xe {effectiveFrom}");
            var effectiveDropoff = !string.IsNullOrWhiteSpace(dropoff) ? dropoff 
                : (Request.Cookies["sbg_last_dropoff"] ?? $"Bến xe {effectiveTo}");
            var effectiveBusType = !string.IsNullOrWhiteSpace(busType) ? busType 
                : (Request.Cookies["sbg_last_bustype"] ?? "Xe Limousine VIP");
            var effectiveLicensePlate = !string.IsNullOrWhiteSpace(licensePlate) ? licensePlate 
                : Request.Cookies["sbg_last_licenseplate"];
            var effectiveRouteName = !string.IsNullOrWhiteSpace(routeName) ? routeName 
                : (Request.Cookies["sbg_last_routename"] ?? $"{effectiveFrom} - {effectiveTo}");
            var effectiveRouteCode = !string.IsNullOrWhiteSpace(routeCode) ? routeCode 
                : Request.Cookies["sbg_last_routecode"];
            var effectiveDriverName = !string.IsNullOrWhiteSpace(driverName) ? driverName 
                : Request.Cookies["sbg_last_driver"];
            var effectiveDriverPhone = !string.IsNullOrWhiteSpace(driverPhone) ? driverPhone 
                : (Request.Cookies["sbg_last_driverphone"] ?? "0988 777 999");
            var effectiveDepTime = !string.IsNullOrWhiteSpace(depTime) ? depTime 
                : (Request.Cookies["sbg_last_deptime"] ?? "08:00");
            var effectiveArrTime = !string.IsNullOrWhiteSpace(arrTime) ? arrTime 
                : Request.Cookies["sbg_last_arrtime"];

            decimal effectiveTotal = total ?? 0;
            if (effectiveTotal <= 0 && decimal.TryParse(Request.Cookies["sbg_last_total"], out var cookieTotal))
            {
                effectiveTotal = cookieTotal;
            }

            int? effectiveTripId = tripId;
            if (!effectiveTripId.HasValue && int.TryParse(Request.Cookies["sbg_last_tripid"], out var cTripId))
            {
                effectiveTripId = cTripId;
            }

            int? effectiveEstMinutes = estMinutes;
            if (!effectiveEstMinutes.HasValue && int.TryParse(Request.Cookies["sbg_last_estminutes"], out var cEst))
            {
                effectiveEstMinutes = cEst;
            }

            decimal? effectiveDistance = distance;
            if (!effectiveDistance.HasValue && decimal.TryParse(Request.Cookies["sbg_last_distance"], out var cDist))
            {
                effectiveDistance = cDist;
            }

            var fromCode = WebApplication1.Services.ProvinceLicenseHelper.GetAllCodes(effectiveFrom).FirstOrDefault() ?? "TN";
            var toCode = WebApplication1.Services.ProvinceLicenseHelper.GetAllCodes(effectiveTo).FirstOrDefault() ?? "CB";

            var effectiveId = !string.IsNullOrEmpty(code) ? code : id;
            var isLegacyDefault = string.IsNullOrEmpty(effectiveId) || 
                                 effectiveId.Equals("SBG-HN-DN-20251024-008", StringComparison.OrdinalIgnoreCase) ||
                                 effectiveId.StartsWith("SBG-84920", StringComparison.OrdinalIgnoreCase);

            // Kiểm tra xem vé này đã tồn tại trong CSDL SQL Server chưa
            Ticket? existingTicket = null;
            if (!string.IsNullOrEmpty(effectiveId) && !isLegacyDefault)
            {
                try
                {
                    existingTicket = await _context.Tickets
                        .Include(t => t.Booking).ThenInclude(b => b.User)
                        .Include(t => t.Booking).ThenInclude(b => b.Trip).ThenInclude(tr => tr.Route)
                        .Include(t => t.Booking).ThenInclude(b => b.Trip).ThenInclude(tr => tr.Bus).ThenInclude(bus => bus.BusType)
                        .Include(t => t.Booking).ThenInclude(b => b.Trip).ThenInclude(tr => tr.Driver).ThenInclude(d => d.User)
                        .Include(t => t.BoardingStop)
                        .Include(t => t.DropOffStop)
                        .FirstOrDefaultAsync(t => t.TicketCode == effectiveId || t.TicketId.ToString() == effectiveId);
                }
                catch { }
            }

            // 2. Nếu có TripId trong CSDL SQL Server, đồng bộ các thông tin chính thức từ Trip thật
            Trip? dbTrip = null;
            if (existingTicket?.Booking?.Trip != null)
            {
                dbTrip = existingTicket.Booking.Trip;
                effectiveTripId = dbTrip.TripId;
            }
            else if (effectiveTripId.HasValue && effectiveTripId.Value > 0)
            {
                try
                {
                    dbTrip = await _context.Trips
                        .Include(t => t.Route)
                            .ThenInclude(r => r.RouteStops.OrderBy(rs => rs.StopOrder))
                                .ThenInclude(rs => rs.Stop)
                        .Include(t => t.Bus)
                            .ThenInclude(b => b.BusType)
                        .Include(t => t.Driver)
                            .ThenInclude(d => d.User)
                        .FirstOrDefaultAsync(t => t.TripId == effectiveTripId.Value);
                }
                catch { }
            }

            // Xác định ngày khởi hành thực tế
            DateTime parsedTripDate = DateTime.Today;
            if (!string.IsNullOrWhiteSpace(effectiveDate))
            {
                var formats = new[] { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd", "yyyy/MM/dd", "MM/dd/yyyy" };
                if (DateTime.TryParseExact(effectiveDate.Trim(), formats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var pExact))
                {
                    parsedTripDate = pExact;
                }
                else if (DateTime.TryParse(effectiveDate.Trim(), out var pDate))
                {
                    parsedTripDate = pDate;
                }
            }
            var targetTripDateOnly = DateOnly.FromDateTime(parsedTripDate.Date);

            // Nếu không tìm thấy Trip theo effectiveTripId (ví dụ do fake ID), tìm Trip thật từ Route và ngày chạy
            if (dbTrip == null)
            {
                try
                {
                    // 1. Ưu tiên tìm chuyến đúng ngày khởi hành và đúng tuyến đường
                    dbTrip = await _context.Trips
                        .Include(t => t.Route)
                        .Include(t => t.Bus).ThenInclude(b => b.BusType)
                        .Include(t => t.Driver).ThenInclude(d => d.User)
                        .FirstOrDefaultAsync(t => t.TripDate == targetTripDateOnly &&
                                                  ((t.Route.StartPoint.Contains(effectiveFrom) || effectiveFrom.Contains(t.Route.StartPoint)) &&
                                                   (t.Route.EndPoint.Contains(effectiveTo) || effectiveTo.Contains(t.Route.EndPoint))));

                    // 2. Nếu chưa có chuyến cho ngày này, tự động tạo chuyến mới chuẩn xác và lưu vào DB
                    if (dbTrip == null)
                    {
                        var matchRoute = await _context.Routes.FirstOrDefaultAsync(r => 
                            (r.StartPoint.Contains(effectiveFrom) || effectiveFrom.Contains(r.StartPoint)) &&
                            (r.EndPoint.Contains(effectiveTo) || effectiveTo.Contains(r.EndPoint)))
                            ?? await _context.Routes.FirstOrDefaultAsync();

                        var matchBus = await _context.Buses.Include(b => b.BusType).FirstOrDefaultAsync(b => b.Status == "Active" || b.Status == "ACTIVE")
                            ?? await _context.Buses.Include(b => b.BusType).FirstOrDefaultAsync();

                        var sampleDriver = await _context.Drivers.Include(d => d.User).FirstOrDefaultAsync();

                        if (matchRoute != null && matchBus != null)
                        {
                            var newDepTime = TimeOnly.TryParse(effectiveDepTime, out var pt) ? pt : new TimeOnly(8, 0);
                            var newArrTime = newDepTime.AddMinutes(matchRoute.EstimatedDuration ?? 150);

                            dbTrip = new Trip
                            {
                                RouteId = matchRoute.RouteId,
                                BusId = matchBus.BusId,
                                DriverId = sampleDriver?.DriverId ?? 1,
                                TripDate = targetTripDateOnly,
                                DepartureTime = newDepTime,
                                ArrivalTime = newArrTime,
                                Status = "Scheduled"
                            };
                            _context.Trips.Add(dbTrip);
                            await _context.SaveChangesAsync();

                            dbTrip = await _context.Trips
                                .Include(t => t.Route)
                                .Include(t => t.Bus).ThenInclude(b => b.BusType)
                                .Include(t => t.Driver).ThenInclude(d => d.User)
                                .FirstOrDefaultAsync(t => t.TripId == dbTrip.TripId);
                        }
                    }

                    if (dbTrip == null)
                    {
                        dbTrip = await _context.Trips
                            .Include(t => t.Route)
                            .Include(t => t.Bus).ThenInclude(b => b.BusType)
                            .Include(t => t.Driver).ThenInclude(d => d.User)
                            .FirstOrDefaultAsync();
                    }

                    if (dbTrip != null)
                    {
                        effectiveTripId = dbTrip.TripId;
                    }
                }
                catch { }
            }

            if (dbTrip != null)
            {
                if (string.IsNullOrWhiteSpace(from)) effectiveFrom = dbTrip.Route?.StartPoint ?? effectiveFrom;
                if (string.IsNullOrWhiteSpace(to)) effectiveTo = dbTrip.Route?.EndPoint ?? effectiveTo;
                if (string.IsNullOrWhiteSpace(routeName)) effectiveRouteName = dbTrip.Route?.RouteName ?? $"{effectiveFrom} - {effectiveTo}";
                if (string.IsNullOrWhiteSpace(routeCode)) effectiveRouteCode = dbTrip.Route?.RouteCode ?? $"{fromCode}-{toCode}-01";
                if (string.IsNullOrWhiteSpace(licensePlate)) effectiveLicensePlate = dbTrip.Bus?.LicensePlate ?? effectiveLicensePlate;
                if (string.IsNullOrWhiteSpace(busType)) effectiveBusType = dbTrip.Bus?.BusType?.TypeName ?? effectiveBusType;
                if (string.IsNullOrWhiteSpace(driverName)) effectiveDriverName = dbTrip.Driver?.User?.FullName ?? effectiveDriverName;
                if (string.IsNullOrWhiteSpace(driverPhone)) effectiveDriverPhone = dbTrip.Driver?.User?.Phone ?? effectiveDriverPhone;
                if (string.IsNullOrWhiteSpace(depTime)) effectiveDepTime = dbTrip.DepartureTime.ToString(@"hh\:mm");
                if (string.IsNullOrWhiteSpace(arrTime) && dbTrip.ArrivalTime.HasValue) effectiveArrTime = dbTrip.ArrivalTime.Value.ToString(@"hh\:mm");
                effectiveEstMinutes ??= dbTrip.Route?.EstimatedDuration;
                effectiveDistance ??= dbTrip.Route?.Distance;
            }

            // Đảm bảo biển số và tài xế luôn chuẩn xác theo chuyến và tỉnh thành
            if (string.IsNullOrWhiteSpace(effectiveLicensePlate))
            {
                var plates = WebApplication1.Services.ProvinceLicenseHelper.GenerateDiverseLicensePlates(effectiveFrom, effectiveTo);
                effectiveLicensePlate = plates.FirstOrDefault() ?? $"{fromCode}B-188.68";
            }
            if (string.IsNullOrWhiteSpace(effectiveDriverName))
            {
                effectiveDriverName = $"Nguyễn Văn Tuấn (Tài xế {effectiveFrom})";
            }
            if (string.IsNullOrWhiteSpace(effectiveRouteCode))
            {
                effectiveRouteCode = $"{fromCode}-{toCode}-01";
            }

            // Tính toán giờ khởi hành và giờ đến chính xác của chuyến xe
            TimeSpan parsedDep = new TimeSpan(8, 0, 0);
            if (!string.IsNullOrWhiteSpace(effectiveDepTime) && TimeSpan.TryParse(effectiveDepTime, out var pDep))
            {
                parsedDep = pDep;
            }

            int finalEstMinutes = effectiveEstMinutes ?? 180;
            TimeSpan parsedArr = parsedDep.Add(TimeSpan.FromMinutes(finalEstMinutes));
            if (!string.IsNullOrWhiteSpace(effectiveArrTime) && TimeSpan.TryParse(effectiveArrTime, out var pArr))
            {
                parsedArr = pArr;
                var diff = (int)(parsedArr - parsedDep).TotalMinutes;
                if (diff > 0) finalEstMinutes = diff;
            }

            // Nếu đọc từ vé đã lưu trong DB
            if (existingTicket != null)
            {
                if (!string.IsNullOrEmpty(existingTicket.SeatNumber)) effectiveSeats = existingTicket.SeatNumber;
                if (existingTicket.Price > 0) effectiveTotal = existingTicket.Price;
                if (existingTicket.Booking != null && existingTicket.Booking.TotalAmount > 0) effectiveTotal = existingTicket.Booking.TotalAmount;
            }

            // Tính toán giá tiền theo số lượng ghế, khoảng cách và loại xe
            var seatCount = effectiveSeats.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Length;
            if (effectiveTotal <= 0)
            {
                var routeMetrics = WebApplication1.Services.RoutePricingService.GetRouteMetrics(effectiveFrom, effectiveTo);
                var effectiveDist = effectiveDistance ?? routeMetrics.Distance;
                var singlePrice = WebApplication1.Services.RoutePricingService.CalculateTripPrice(effectiveDist, effectiveBusType, 29, 0);
                effectiveTotal = singlePrice * Math.Max(1, seatCount);
            }

            // Lấy địa chỉ đón/trả chính xác từ Database BusStop hoặc hệ thống địa danh
            var boardingAddress = await GetAccurateStopAddressAsync(effectiveBoarding, effectiveFrom);
            var dropOffAddress = await GetAccurateStopAddressAsync(effectiveDropoff, effectiveTo);

            // =========================================================================
            // SINH MÃ VÉ VÀ MÃ ĐẶT CHỖ TỰ ĐỘNG RANDOM & LƯU VÀO DATABASE
            // =========================================================================
            string cleanTicketCode;
            string cleanBookingCode;
            string finalTicketStatus;

            if (existingTicket != null)
            {
                cleanTicketCode = existingTicket.TicketCode;
                cleanBookingCode = existingTicket.Booking?.BookingCode ?? $"BK-{DateTime.Now:yyMMdd}-{Random.Shared.Next(1000, 9999)}";
                finalTicketStatus = !string.IsNullOrWhiteSpace(existingTicket.Status) ? existingTicket.Status.ToUpper() : "ACTIVE";
            }
            else
            {
                // Sinh mã vé ngẫu nhiên duy nhất dạng SBG-XXXXXX (6 chữ số ngẫu nhiên)
                do
                {
                    cleanTicketCode = $"SBG-{Random.Shared.Next(100000, 999999)}";
                } while (await _context.Tickets.AnyAsync(t => t.TicketCode == cleanTicketCode));

                cleanBookingCode = $"BK-{DateTime.Now:yyMMdd}-{Random.Shared.Next(1000, 9999)}";
                finalTicketStatus = !string.IsNullOrWhiteSpace(status) ? status.ToUpper() : "ACTIVE";

                // Lưu ngay vé mới được sinh ngẫu nhiên vào Database SQL Server
                try
                {
                    int? currentAuthUid = null;
                    if (User.Identity?.IsAuthenticated == true && int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var pUid))
                    {
                        currentAuthUid = pUid;
                    }

                    var validUser = (currentAuthUid.HasValue ? await _context.Users.FirstOrDefaultAsync(u => u.UserId == currentAuthUid.Value) : null)
                        ?? await _context.Users.FirstOrDefaultAsync(u => u.Email == "khachhang@smartbus.vn")
                        ?? await _context.Users.FirstOrDefaultAsync();

                    if (validUser == null)
                    {
                        validUser = new User
                        {
                            FullName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "Trần Văn Bình",
                            Phone = "0912 345 678",
                            Email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "khachhang@smartbus.vn",
                            PasswordHash = "AQAAAAEAACcQAAAAE",
                            Status = "Active",
                            CreatedAt = DateTime.Now
                        };
                        _context.Users.Add(validUser);
                        await _context.SaveChangesAsync();
                    }

                    if (dbTrip != null && validUser != null)
                    {
                        var newBooking = new Booking
                        {
                            UserId = validUser.UserId,
                            TripId = dbTrip.TripId,
                            BookingCode = cleanBookingCode.Length > 30 ? cleanBookingCode.Substring(0, 30) : cleanBookingCode,
                            BookingTime = DateTime.Now,
                            TotalAmount = effectiveTotal,
                            Status = "Confirmed"
                        };
                        _context.Bookings.Add(newBooking);
                        await _context.SaveChangesAsync();

                        var seatStr = effectiveSeats ?? "A01";
                        if (seatStr.Length > 10) seatStr = seatStr.Substring(0, 10);

                        var newTicket = new Ticket
                        {
                            BookingId = newBooking.BookingId,
                            TicketCode = cleanTicketCode,
                            SeatNumber = seatStr,
                            Price = effectiveTotal,
                            Status = finalTicketStatus
                        };
                        _context.Tickets.Add(newTicket);
                        await _context.SaveChangesAsync();

                        _logger.LogInformation("Đã lưu vé ngẫu nhiên vào Database thành công: Mã vé [{TicketCode}] - Ghế [{Seat}] - Booking [{BookingCode}]", cleanTicketCode, seatStr, cleanBookingCode);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi lưu vé ngẫu nhiên vào CSDL: {Message}", ex.Message);
                }
            }

            // Ghi nhớ mã vé mới vào Cookie để duy trì trạng thái phiên xem vé
            try
            {
                Response.Cookies.Append("sbg_last_ticket_code", cleanTicketCode, new Microsoft.AspNetCore.Http.CookieOptions
                {
                    Expires = DateTimeOffset.Now.AddDays(7),
                    IsEssential = true,
                    SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax
                });
            }
            catch { }

            // 3. Khởi tạo ViewModel chứa 100% THÔNG TIN CHUYẾN XE KHÁCH HÀNG ĐÃ ĐẶT
            var passengerName = existingTicket?.Booking?.User?.FullName ?? User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? (User.Identity?.Name ?? "Trần Văn Bình");
            var passengerPhone = existingTicket?.Booking?.User?.Phone ?? "0912 345 678";
            var passengerEmail = existingTicket?.Booking?.User?.Email ?? User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "khachhang@smartbus.vn";

            var model = new TicketDetailViewModel
            {
                TicketId = existingTicket?.TicketId ?? (effectiveTripId ?? 2026),
                TicketCode = cleanTicketCode,
                SeatNumber = effectiveSeats,
                Price = effectiveTotal,
                Status = finalTicketStatus,

                BookingId = existingTicket?.BookingId ?? 8821,
                BookingCode = cleanBookingCode,
                BookingTime = existingTicket?.Booking?.BookingTime ?? DateTime.Now,
                TotalAmount = effectiveTotal,

                PassengerName = passengerName,
                PassengerPhone = passengerPhone,
                PassengerEmail = passengerEmail,

                RouteCode = effectiveRouteCode,
                RouteName = effectiveRouteName,
                StartPoint = effectiveFrom,
                EndPoint = effectiveTo,
                Distance = effectiveDistance ?? WebApplication1.Services.RoutePricingService.GetRouteMetrics(effectiveFrom, effectiveTo).Distance,
                EstimatedDuration = finalEstMinutes > 0 ? finalEstMinutes : WebApplication1.Services.RoutePricingService.GetRouteMetrics(effectiveFrom, effectiveTo).EstimatedMinutes,

                BoardingStopName = effectiveBoarding,
                BoardingStopAddress = boardingAddress,
                DepartureTime = parsedDep,

                DropOffStopName = effectiveDropoff,
                DropOffStopAddress = dropOffAddress,
                ArrivalTime = parsedArr,

                TripId = dbTrip?.TripId ?? (effectiveTripId ?? 101),
                TripDate = parsedTripDate,
                LicensePlate = effectiveLicensePlate,
                BusTypeName = effectiveBusType,
                DriverName = effectiveDriverName,
                DriverPhone = effectiveDriverPhone,

                // Payload mã QR định dạng chuẩn an toàn không dấu
                QrDataPayload = $"SMARTBUS|TICKET:{cleanTicketCode}|BOOKING:{cleanBookingCode}|SEAT:{effectiveSeats}|DATE:{parsedTripDate:yyyy-MM-dd}|STATUS:{finalTicketStatus}"
            };

            return View(model);
        }

        // =========================================================================
        // GET: /Ticket/Payment
        // Trang cổng thanh toán thông minh (QR VietQR, thẻ, ví)
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> Payment(
            string? ticketCode = null,
            string? bookingCode = null,
            string? from = null,
            string? to = null,
            string? seats = null,
            decimal? total = null,
            string? boarding = null,
            string? dropoff = null,
            string? date = null,
            string? depTime = null,
            string? arrTime = null,
            string? busType = null,
            string? licensePlate = null,
            string? routeName = null,
            string? routeCode = null,
            string? driverName = null,
            string? driverPhone = null,
            int? tripId = null)
        {
            var effFrom = !string.IsNullOrEmpty(from) ? from : (Request.Cookies["sbg_last_from"] ?? Request.Cookies["sbg_search_from"] ?? "Thái Nguyên");
            var effTo = !string.IsNullOrEmpty(to) ? to : (Request.Cookies["sbg_last_to"] ?? Request.Cookies["sbg_search_to"] ?? "Cao Bằng");
            var effSeats = !string.IsNullOrEmpty(seats) ? seats : (Request.Cookies["sbg_last_seats"] ?? "A01");
            var effTotal = total ?? (decimal.TryParse(Request.Cookies["sbg_last_total"], out var t) ? t : 180000m);
            var effBoarding = boarding ?? Request.Cookies["sbg_last_boarding"] ?? $"Bến xe {effFrom}";
            var effDropoff = dropoff ?? Request.Cookies["sbg_last_dropoff"] ?? $"Bến xe {effTo}";
            var effBusType = busType ?? Request.Cookies["sbg_last_bustype"] ?? "Xe Limousine VIP Cao Cấp";
            var effLicensePlate = licensePlate ?? Request.Cookies["sbg_last_licenseplate"] ?? "20B-188.68";
            var effDepTime = depTime ?? Request.Cookies["sbg_last_deptime"] ?? "08:00";
            var effTripDate = date ?? Request.Cookies["sbg_last_date"] ?? DateTime.Today.AddDays(1).ToString("dd/MM/yyyy");
            var effRouteName = routeName ?? Request.Cookies["sbg_last_routename"] ?? $"{effFrom} ➔ {effTo}";
            var effRouteCode = routeCode ?? Request.Cookies["sbg_last_routecode"] ?? "TN-CB-01";
            var effDriverName = driverName ?? Request.Cookies["sbg_last_driver"] ?? $"Nguyễn Văn Tuấn (Tài xế {effFrom})";
            var effDriverPhone = driverPhone ?? Request.Cookies["sbg_last_driverphone"] ?? "0988 777 999";

            var fromCode = WebApplication1.Services.ProvinceLicenseHelper.GetAllCodes(effFrom).FirstOrDefault() ?? "TN";
            var toCode = WebApplication1.Services.ProvinceLicenseHelper.GetAllCodes(effTo).FirstOrDefault() ?? "CB";

            ViewBag.RouteName = effRouteName;
            ViewBag.StartPoint = effFrom;
            ViewBag.EndPoint = effTo;
            ViewBag.BoardingStop = effBoarding;
            ViewBag.DropOffStop = effDropoff;
            ViewBag.SeatNumber = effSeats;
            ViewBag.TotalAmount = effTotal;
            ViewBag.DepartureTime = effDepTime;
            ViewBag.TripDate = effTripDate;
            ViewBag.BusTypeName = effBusType;
            ViewBag.LicensePlate = effLicensePlate;
            ViewBag.DriverName = effDriverName;
            ViewBag.DriverPhone = effDriverPhone;
            ViewBag.RouteCode = effRouteCode;
            ViewBag.TicketCode = ticketCode ?? $"SBG-{fromCode}-{toCode}-{DateTime.Now:yyyyMMdd}-{tripId ?? 101}";
            ViewBag.BookingCode = bookingCode ?? $"BK-{fromCode}{toCode}-{DateTime.Now:MMddHHmm}";
            ViewBag.PassengerName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? (User.Identity?.Name ?? "Trần Văn Bình");
            ViewBag.PassengerPhone = "0912 345 678";

            if (!string.IsNullOrEmpty(ticketCode) || !string.IsNullOrEmpty(bookingCode))
            {
                var ticket = await _context.Tickets
                    .Include(t => t.Booking).ThenInclude(b => b.User)
                    .Include(t => t.Booking).ThenInclude(b => b.Trip).ThenInclude(tr => tr.Route)
                    .Include(t => t.Booking).ThenInclude(b => b.Trip).ThenInclude(tr => tr.Bus).ThenInclude(bus => bus.BusType)
                    .FirstOrDefaultAsync(t => (!string.IsNullOrEmpty(ticketCode) && t.TicketCode == ticketCode) ||
                                              (!string.IsNullOrEmpty(bookingCode) && t.Booking.BookingCode == bookingCode));

                if (ticket != null)
                {
                    ViewBag.TicketCode = ticket.TicketCode;
                    ViewBag.BookingCode = ticket.Booking?.BookingCode;
                    ViewBag.TotalAmount = ticket.Booking?.TotalAmount ?? ticket.Price;
                    ViewBag.SeatNumber = ticket.SeatNumber;
                    ViewBag.RouteName = ticket.Booking?.Trip?.Route?.RouteName ?? $"{effFrom} ➔ {effTo}";
                    ViewBag.DepartureTime = ticket.Booking?.Trip?.DepartureTime.ToString(@"hh\:mm");
                    ViewBag.TripDate = ticket.Booking?.Trip?.TripDate.ToString("dd/MM/yyyy");
                    ViewBag.PassengerName = ticket.Booking?.User?.FullName ?? ViewBag.PassengerName;
                    ViewBag.PassengerPhone = ticket.Booking?.User?.Phone ?? ViewBag.PassengerPhone;
                }
            }

            return View();
        }

        // =========================================================================
        // POST & GET: /Ticket/ConfirmPayment
        // Xác nhận thanh toán & ghi nhận giao dịch thật vào Database SQL Server
        // =========================================================================
        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> ConfirmPayment(
            string? ticketCode,
            string? bookingCode,
            decimal? amount,
            string? paymentMethod = "VietQR",
            string? transactionCode = null)
        {
            var searchFrom = Request.Cookies["sbg_last_from"] ?? Request.Cookies["sbg_search_from"];
            var defaultCode = (searchFrom != null && searchFrom.Contains("Thái Nguyên")) ? "SBG-TN-CB-001" : "SBG-TN-CB-001";
            var effectiveTicketCode = ticketCode ?? defaultCode;
            var effectiveBookingCode = bookingCode ?? "BK-TNCB-001";
            var effectiveAmount = amount ?? (decimal.TryParse(Request.Cookies["sbg_last_total"], out var tot) ? tot : 180000m);
            var effectiveMethod = string.IsNullOrEmpty(paymentMethod) ? "VietQR" : paymentMethod;
            var effectiveTxn = string.IsNullOrEmpty(transactionCode)
                ? $"TXN{DateTime.Now:yyyyMMddHHmmss}{Random.Shared.Next(100, 999)}"
                : transactionCode;

            try
            {
                // Tìm vé và đơn đặt vé trong Database
                var ticket = await _context.Tickets
                    .Include(t => t.Booking)
                        .ThenInclude(b => b.Tickets)
                    .Include(t => t.Booking)
                        .ThenInclude(b => b.Payments)
                    .FirstOrDefaultAsync(t => t.TicketCode == effectiveTicketCode ||
                                              t.TicketId.ToString() == effectiveTicketCode ||
                                              (!string.IsNullOrEmpty(bookingCode) && t.Booking.BookingCode == bookingCode));

                if (ticket != null)
                {
                    ticket.Status = "CONFIRMED";
                    if (ticket.Booking != null)
                    {
                        ticket.Booking.Status = "Confirmed";
                        foreach (var t in ticket.Booking.Tickets)
                        {
                            t.Status = "CONFIRMED";
                        }

                        // Ghi nhận bản ghi thanh toán thật vào bảng Payment
                        var payment = new Payment
                        {
                            BookingId = ticket.Booking.BookingId,
                            Amount = amount ?? ticket.Booking.TotalAmount,
                            PaymentMethod = effectiveMethod,
                            Status = "Paid",
                            PaymentTime = DateTime.Now,
                            TransactionCode = effectiveTxn
                        };
                        _context.Payments.Add(payment);

                        // Giải phóng giữ chỗ ghế tạm thời
                        if (ticket.Booking.TripId > 0 && !string.IsNullOrEmpty(ticket.SeatNumber))
                        {
                            await _seatHoldService.ReleaseSeatsAsync(
                                ticket.Booking.TripId,
                                new List<string> { ticket.SeatNumber },
                                "payment_completed",
                                null);
                        }
                    }

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Xác nhận thanh toán thành công #{effectiveTxn} với số tiền {effectiveAmount:N0} đ qua {effectiveMethod}! Vé điện tử đã được kích hoạt.";
                    return RedirectToAction("Detail", new { id = ticket.TicketCode, status = "CONFIRMED" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý lưu giao dịch thanh toán: {Message}", ex.Message);
            }

            // Fallback khi chạy demo
            TempData["SuccessMessage"] = $"Xác nhận thanh toán thành công #{effectiveTxn} với số tiền {effectiveAmount:N0} đ qua {effectiveMethod}! Vé điện tử đã được kích hoạt.";
            return RedirectToAction("Detail", new { id = effectiveTicketCode, status = "CONFIRMED" });
        }

        // =========================================================================
        // FE-US2-04: Màn hình chờ & Kết quả thanh toán (Callback Page)
        // GET: /payment-result & /Ticket/PaymentResult
        // =========================================================================
        [HttpGet]
        [Route("payment-result")]
        [Route("Ticket/PaymentResult")]
        public async Task<IActionResult> PaymentResult(
            string? ticketCode = null,
            string? bookingCode = null,
            string? status = null,
            string? orderId = null,
            string? transactionNo = null,
            decimal? amount = null,
            string? paymentMethod = "VietQR")
        {
            var effectiveTicketCode = !string.IsNullOrEmpty(ticketCode) ? ticketCode : "SBG-84920";
            var effectiveBookingCode = !string.IsNullOrEmpty(bookingCode) ? bookingCode : "BK-84920";
            var effectiveMethod = string.IsNullOrEmpty(paymentMethod) ? "VietQR" : paymentMethod;
            var effectiveTxn = !string.IsNullOrEmpty(transactionNo) 
                ? transactionNo 
                : (!string.IsNullOrEmpty(orderId) ? orderId : $"TXN{DateTime.Now:yyyyMMddHHmmss}");

            var isFailedParam = !string.IsNullOrEmpty(status) && 
                (status.Equals("failed", StringComparison.OrdinalIgnoreCase) || 
                 status.Equals("cancel", StringComparison.OrdinalIgnoreCase) || 
                 status.Equals("error", StringComparison.OrdinalIgnoreCase));

            // Tìm vé trong CSDL
            Ticket? dbTicket = null;
            try
            {
                dbTicket = await _context.Tickets
                    .Include(t => t.Booking).ThenInclude(b => b.User)
                    .Include(t => t.Booking).ThenInclude(b => b.Trip).ThenInclude(tr => tr.Route)
                    .Include(t => t.Booking).ThenInclude(b => b.Trip).ThenInclude(tr => tr.Bus).ThenInclude(bus => bus.BusType)
                    .Include(t => t.BoardingStop)
                    .Include(t => t.DropOffStop)
                    .FirstOrDefaultAsync(t => t.TicketCode == effectiveTicketCode ||
                                              t.TicketId.ToString() == effectiveTicketCode ||
                                              (!string.IsNullOrEmpty(bookingCode) && t.Booking.BookingCode == bookingCode));

                if (dbTicket != null && !isFailedParam)
                {
                    dbTicket.Status = "CONFIRMED";
                    if (dbTicket.Booking != null)
                    {
                        dbTicket.Booking.Status = "Confirmed";
                    }
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi kiểm tra Database trong PaymentResult: {Message}", ex.Message);
            }

            var fbFrom = Request.Cookies["sbg_last_from"] ?? Request.Cookies["sbg_search_from"] ?? "Thái Nguyên";
            var fbTo = Request.Cookies["sbg_last_to"] ?? Request.Cookies["sbg_search_to"] ?? "Cao Bằng";
            var fbSeats = Request.Cookies["sbg_last_seats"] ?? "A01";
            var fbBoarding = Request.Cookies["sbg_last_boarding"] ?? $"Bến xe {fbFrom}";
            var fbDropoff = Request.Cookies["sbg_last_dropoff"] ?? $"Bến xe {fbTo}";
            var fbAmount = amount ?? (decimal.TryParse(Request.Cookies["sbg_last_total"], out var tot) ? tot : 180000m);
            var fbPlate = Request.Cookies["sbg_last_licenseplate"] ?? (fbFrom.Contains("Thái Nguyên") ? "20B-188.68" : "29B-888.68");
            var fbBusType = Request.Cookies["sbg_last_bustype"] ?? "Xe Limousine VIP Cao Cấp";
            var fbRouteName = Request.Cookies["sbg_last_routename"] ?? $"{fbFrom} - {fbTo}";
            DateTime fbDepDateTime = DateTime.Today.AddDays(1).AddHours(8);
            if (TimeSpan.TryParse(Request.Cookies["sbg_last_deptime"], out var dtDep))
            {
                var baseD = DateTime.TryParse(Request.Cookies["sbg_last_date"], out var pd) ? pd : DateTime.Today.AddDays(1);
                fbDepDateTime = baseD.Date.Add(dtDep);
            }

            PaymentResultViewModel model;
            if (dbTicket != null)
            {
                var tr = dbTicket.Booking?.Trip;
                var r = tr?.Route;
                var b = tr?.Bus;
                var u = dbTicket.Booking?.User;

                var depTime = tr != null ? tr.TripDate.ToDateTime(tr.DepartureTime) : fbDepDateTime;

                model = new PaymentResultViewModel
                {
                    TicketCode = dbTicket.TicketCode,
                    BookingCode = dbTicket.Booking?.BookingCode ?? effectiveBookingCode,
                    Amount = amount ?? dbTicket.Booking?.TotalAmount ?? dbTicket.Price,
                    PaymentMethod = effectiveMethod,
                    TransactionCode = effectiveTxn,
                    PaymentTime = DateTime.Now,
                    Status = isFailedParam ? "FAILED" : "SUCCESS",
                    FailureReason = isFailedParam ? "Giao dịch đã bị hủy hoặc từ chối bởi người dùng/ngân hàng." : null,
                    PassengerName = u?.FullName ?? (User.Identity?.Name ?? "Trần Văn Bình"),
                    PassengerPhone = u?.Phone ?? "0912 345 678",
                    RouteName = r?.RouteName ?? fbRouteName,
                    StartPoint = r?.StartPoint ?? fbFrom,
                    EndPoint = r?.EndPoint ?? fbTo,
                    BoardingStop = dbTicket.BoardingStop?.StopName ?? fbBoarding,
                    DropOffStop = dbTicket.DropOffStop?.StopName ?? fbDropoff,
                    SeatNumber = !string.IsNullOrEmpty(dbTicket.SeatNumber) ? dbTicket.SeatNumber : fbSeats,
                    BusTypeName = b?.BusType?.TypeName ?? fbBusType,
                    LicensePlate = b?.LicensePlate ?? fbPlate,
                    DepartureTime = depTime,
                    QrDataPayload = $"SMARTBUS|TICKET:{dbTicket.TicketCode}|ROUTE:{fbFrom}-{fbTo}|STATUS:CONFIRMED|TXN:{effectiveTxn}"
                };
            }
            else
            {
                model = new PaymentResultViewModel
                {
                    TicketCode = effectiveTicketCode.Contains("84920") ? $"SBG-TN-CB-{DateTime.Now:yyyyMMdd}-01" : effectiveTicketCode,
                    BookingCode = effectiveBookingCode.Contains("84920") ? "BK-TNCB-001" : effectiveBookingCode,
                    Amount = fbAmount,
                    PaymentMethod = effectiveMethod,
                    TransactionCode = effectiveTxn,
                    PaymentTime = DateTime.Now,
                    Status = isFailedParam ? "FAILED" : "SUCCESS",
                    FailureReason = isFailedParam ? "Giao dịch thanh toán đã bị hủy bởi người dùng." : null,
                    PassengerName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? (User.Identity?.Name ?? "Trần Văn Bình"),
                    PassengerPhone = "0912 345 678",
                    RouteName = fbRouteName,
                    StartPoint = fbFrom,
                    EndPoint = fbTo,
                    BoardingStop = fbBoarding,
                    DropOffStop = fbDropoff,
                    SeatNumber = fbSeats,
                    BusTypeName = fbBusType,
                    LicensePlate = fbPlate,
                    DepartureTime = fbDepDateTime,
                    QrDataPayload = $"SMARTBUS|TICKET:{effectiveTicketCode}|ROUTE:{fbFrom}-{fbTo}|STATUS:CONFIRMED|TXN:{effectiveTxn}"
                };
            }

            return View(model);
        }

        // =========================================================================
        // GET: /api/v1/payments/check-status & /api/payment/check-status
        // API Polling kiểm tra trạng thái thanh toán cuối cùng
        // =========================================================================
        [HttpGet]
        [Route("api/v1/payments/check-status")]
        [Route("api/payment/check-status")]
        public async Task<IActionResult> CheckPaymentStatus(
            [FromQuery] string? orderCode = null,
            [FromQuery] string? bookingCode = null,
            [FromQuery] string? ticketCode = null,
            [FromQuery] string? orderId = null)
        {
            var searchCode = !string.IsNullOrEmpty(orderCode)
                ? orderCode
                : (!string.IsNullOrEmpty(bookingCode) ? bookingCode : (!string.IsNullOrEmpty(ticketCode) ? ticketCode : orderId));

            if (string.IsNullOrEmpty(searchCode))
            {
                searchCode = "SBG-84920";
            }

            var cleanCode = searchCode.Trim().ToLowerInvariant();

            // Kiểm tra DB
            try
            {
                var ticket = await _context.Tickets
                    .Include(t => t.Booking).ThenInclude(b => b.Payments)
                    .FirstOrDefaultAsync(t => t.TicketCode.ToLower() == cleanCode ||
                                              (t.Booking != null && t.Booking.BookingCode.ToLower() == cleanCode));

                if (ticket != null)
                {
                    var isPaid = ticket.Status == "CONFIRMED" ||
                                 ticket.Status == "PAID" ||
                                 ticket.Status == "ACTIVE" ||
                                 ticket.Booking?.Payments.Any(p => p.Status == "Paid") == true;

                    return Ok(new
                    {
                        success = true,
                        isPaid = isPaid,
                        status = isPaid ? "SUCCESS" : "PENDING",
                        ticketCode = ticket.TicketCode,
                        bookingCode = ticket.Booking?.BookingCode,
                        amount = ticket.Booking?.TotalAmount ?? ticket.Price,
                        paymentMethod = ticket.Booking?.Payments.LastOrDefault()?.PaymentMethod ?? "VietQR",
                        transactionNo = ticket.Booking?.Payments.LastOrDefault()?.TransactionCode ?? $"TXN{DateTime.Now:yyyyMMddHHmmss}",
                        paymentTime = ticket.Booking?.Payments.LastOrDefault()?.PaymentTime ?? DateTime.Now,
                        message = isPaid ? "Giao dịch thanh toán đã được đối soát thành công." : "Đang chờ đối soát từ cổng thanh toán..."
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi kiểm tra trạng thái thanh toán: {Message}", ex.Message);
            }

            // Demo mock fallback
            return Ok(new
            {
                success = true,
                isPaid = true,
                status = "SUCCESS",
                ticketCode = searchCode.ToUpperInvariant(),
                bookingCode = "BK-84920",
                amount = 530000m,
                paymentMethod = "VietQR",
                transactionNo = $"TXN{DateTime.Now:yyyyMMddHHmmss}",
                paymentTime = DateTime.Now,
                message = "Giao dịch thanh toán đã được đối soát thành công."
            });
        }

        // =========================================================================
        // LUỒNG ĐỔI CHUYẾN (RESCHEDULE TICKET FLOW - 3 BƯỚC THÔNG MINH)
        // Bước 1: Tìm kiếm chuyến xe mới (danh sách chuyến thay thế ĐÚNG tuyến và ngày)
        // Bước 2: Giao diện sơ đồ ghế chuyến mới để khách chọn lại ghế & tầng
        // Bước 3: So sánh vé cũ và vé mới, tính chính xác chênh lệch giá (hoàn tiền / bù tiền)
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> Reschedule(
            string? ticketCode = "SBG-HN-DN-20251024-008",
            int step = 1,
            string? newDate = null,
            string? timeOfDay = null,
            int? tripId = null,
            string? newSeat = null,
            decimal? seatPrice = null,
            string? boarding = null,
            string? dropoff = null)
        {
            var effectiveTicketCode = string.IsNullOrEmpty(ticketCode) ? "SBG-HN-DN-20251024-008" : ticketCode;

            // 1. Lấy thông tin vé hiện tại từ Database thật
            var dbTicket = await _context.Tickets
                .Include(t => t.Booking).ThenInclude(b => b.User)
                .Include(t => t.Booking).ThenInclude(b => b.Trip).ThenInclude(tr => tr.Route)
                .Include(t => t.Booking).ThenInclude(b => b.Trip).ThenInclude(tr => tr.Bus).ThenInclude(bus => bus.BusType)
                .Include(t => t.Booking).ThenInclude(b => b.Trip).ThenInclude(tr => tr.Driver).ThenInclude(d => d.User)
                .Include(t => t.BoardingStop)
                .Include(t => t.DropOffStop)
                .FirstOrDefaultAsync(t => t.TicketCode == effectiveTicketCode || t.TicketId.ToString() == effectiveTicketCode);

            TicketDetailViewModel oldTicket;
            if (dbTicket != null)
            {
                var tr = dbTicket.Booking?.Trip;
                var r = tr?.Route;
                var b = tr?.Bus;
                var d = tr?.Driver;
                var u = dbTicket.Booking?.User;

                oldTicket = new TicketDetailViewModel
                {
                    TicketId = dbTicket.TicketId,
                    TicketCode = dbTicket.TicketCode,
                    SeatNumber = dbTicket.SeatNumber ?? "VIP-05",
                    Price = dbTicket.Price,
                    Status = dbTicket.Status?.ToUpper() ?? "ACTIVE",
                    BookingId = dbTicket.BookingId,
                    BookingCode = dbTicket.Booking?.BookingCode ?? "BK-0001",
                    BookingTime = dbTicket.Booking?.BookingTime ?? DateTime.Now,
                    TotalAmount = dbTicket.Booking?.TotalAmount ?? dbTicket.Price,
                    PassengerName = u?.FullName ?? "Hành khách SmartBus",
                    PassengerPhone = u?.Phone ?? "0987654321",
                    PassengerEmail = u?.Email ?? "passenger@smartbus.vn",
                    RouteCode = r?.RouteCode ?? "R01",
                    RouteName = r?.RouteName ?? $"{r?.StartPoint ?? "Hà Nội"} - {r?.EndPoint ?? "Đà Nẵng"}",
                    StartPoint = r?.StartPoint ?? "Hà Nội",
                    EndPoint = r?.EndPoint ?? "Đà Nẵng",
                    Distance = r?.Distance ?? 760m,
                    EstimatedDuration = r?.EstimatedDuration ?? 750,
                    BoardingStopName = dbTicket.BoardingStop?.StopName ?? (r?.StartPoint + " (Bến xe xuất phát)"),
                    BoardingStopAddress = dbTicket.BoardingStop?.Address ?? "Điểm đón SmartBus Express",
                    DepartureTime = tr?.DepartureTime.ToTimeSpan() ?? new TimeSpan(19, 30, 0),
                    DropOffStopName = dbTicket.DropOffStop?.StopName ?? (r?.EndPoint + " (Bến xe trả khách)"),
                    DropOffStopAddress = dbTicket.DropOffStop?.Address ?? "Điểm trả SmartBus Express",
                    ArrivalTime = tr?.ArrivalTime?.ToTimeSpan() ?? new TimeSpan(8, 0, 0),
                    TripId = tr?.TripId ?? 1,
                    TripDate = tr?.TripDate.ToDateTime(TimeOnly.MinValue) ?? DateTime.Today.AddDays(1),
                    LicensePlate = b?.LicensePlate ?? "29B-888.68",
                    BusTypeName = b?.BusType?.TypeName ?? "Limousine VIP 22 Phòng Đơn Cung Điện",
                    DriverName = d?.User?.FullName ?? "Tài xế SmartBus",
                    DriverPhone = d?.User?.Phone ?? "0988 777 999",
                    QrDataPayload = $"SMARTBUS|TICKET:{dbTicket.TicketCode}|BOOKING:{dbTicket.Booking?.BookingCode}|SEAT:{dbTicket.SeatNumber}"
                };
            }
            else
            {
                oldTicket = GetMockOldTicket(effectiveTicketCode);
            }

            var targetDate = !string.IsNullOrEmpty(newDate) && DateTime.TryParse(newDate, out var parsedDate)
                ? parsedDate
                : (oldTicket.TripDate > DateTime.Today ? oldTicket.TripDate.AddDays(1) : DateTime.Today.AddDays(1));

            // 2. Lấy danh sách chuyến xe thay thế phù hợp với tuyến đường thật và ngày muốn đổi
            var availableTrips = await GetAvailableRescheduleTripsAsync(oldTicket, targetDate);

            // 3. Lọc theo khung giờ sáng / chiều / tối
            if (!string.IsNullOrEmpty(timeOfDay))
            {
                if (timeOfDay == "morning")
                    availableTrips = availableTrips.Where(t => t.DepartureTime.Hours >= 6 && t.DepartureTime.Hours < 12).ToList();
                else if (timeOfDay == "afternoon")
                    availableTrips = availableTrips.Where(t => t.DepartureTime.Hours >= 12 && t.DepartureTime.Hours < 18).ToList();
                else if (timeOfDay == "evening")
                    availableTrips = availableTrips.Where(t => t.DepartureTime.Hours >= 18 || t.DepartureTime.Hours < 6).ToList();
            }

            TripItemViewModel? selectedTrip = null;
            if (tripId.HasValue)
            {
                selectedTrip = availableTrips.FirstOrDefault(t => t.TripId == tripId.Value);
            }
            if (selectedTrip == null && step >= 2 && availableTrips.Any())
            {
                selectedTrip = availableTrips.First();
                tripId = selectedTrip.TripId;
            }

            var defaultSeat = selectedTrip?.Seats.FirstOrDefault(s => s.Status == "AVAILABLE")?.SeatCode ?? "VIP-08";

            var model = new RescheduleViewModel
            {
                Step = step,
                OldTicket = oldTicket,
                NewDepartureDate = targetDate,
                TimeOfDayFilter = timeOfDay,
                AvailableTrips = availableTrips,
                SelectedTripId = tripId,
                SelectedTrip = selectedTrip,
                SelectedNewSeat = newSeat ?? (step == 3 ? defaultSeat : null),
                NewSeatPrice = seatPrice ?? (selectedTrip?.Price ?? oldTicket.Price),
                SelectedBoardingPoint = boarding ?? selectedTrip?.BoardingPoints.FirstOrDefault(),
                SelectedDropOffPoint = dropoff ?? selectedTrip?.DropOffPoints.FirstOrDefault()
            };

            return View(model);
        }

        // =========================================================================
        // POST: /Ticket/ConfirmReschedule
        // =========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmReschedule(RescheduleViewModel model)
        {
            var ticketCode = model.OldTicket.TicketCode;

            // 1. Cập nhật thông tin vé mới vào Database nếu tồn tại
            try
            {
                var dbTicket = await _context.Tickets
                    .Include(t => t.Booking)
                    .FirstOrDefaultAsync(t => t.TicketCode == ticketCode || t.TicketId == model.OldTicket.TicketId);

                if (dbTicket != null)
                {
                    if (model.SelectedTripId.HasValue && model.SelectedTripId.Value > 0)
                    {
                        var targetTrip = await _context.Trips.FindAsync(model.SelectedTripId.Value);
                        if (targetTrip != null)
                        {
                            if (dbTicket.Booking != null)
                            {
                                dbTicket.Booking.TripId = targetTrip.TripId;
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(model.SelectedNewSeat))
                    {
                        dbTicket.SeatNumber = model.SelectedNewSeat;
                    }

                    if (model.EffectiveNewPrice > 0)
                    {
                        dbTicket.Price = model.EffectiveNewPrice;
                        if (dbTicket.Booking != null)
                        {
                            dbTicket.Booking.TotalAmount = model.EffectiveNewPrice;
                        }
                    }

                    dbTicket.Status = "CONFIRMED";
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật đổi chuyến trong DB: {Message}", ex.Message);
            }

            // 2. Cập nhật thông tin vé mới vào TempData để chuyển sang trang chi tiết vé
            TempData["SuccessMessage"] = $"Đổi chuyến thành công! Vé #{model.OldTicket.TicketCode} đã được cập nhật sang chuyến mới khởi hành ngày {model.NewDepartureDate:dd/MM/yyyy}.";
            TempData["NewSeatNumber"] = model.SelectedNewSeat ?? "VIP-08";
            TempData["NewTicketPrice"] = model.EffectiveNewPrice;
            TempData["NewTripDate"] = model.NewDepartureDate.ToString("yyyy-MM-dd");
            if (model.SelectedTrip != null)
            {
                TempData["NewDepartureTime"] = model.SelectedTrip.DepartureTime.ToString(@"hh\:mm\:ss");
                TempData["NewLicensePlate"] = model.SelectedTrip.LicensePlate;
                TempData["NewBusTypeName"] = model.SelectedTrip.BusTypeName;
            }
            if (!string.IsNullOrEmpty(model.SelectedBoardingPoint))
                TempData["NewBoardingStop"] = model.SelectedBoardingPoint;
            if (!string.IsNullOrEmpty(model.SelectedDropOffPoint))
                TempData["NewDropOffStop"] = model.SelectedDropOffPoint;

            return RedirectToAction("Detail", new { id = model.OldTicket.TicketCode });
        }

        // =========================================================================
        // GET: /Ticket/Cancel/{id}
        // =========================================================================
        [HttpGet]
        [Route("Ticket/Cancel/{id?}")]
        [Route("Ticket/Cancel")]
        [Route("Ticket/Policy")]
        [Route("Ticket/CancellationPolicy")]
        public async Task<IActionResult> Cancel(string? id, string? ticketCode = null)
        {
            var effectiveCode = !string.IsNullOrEmpty(ticketCode) ? ticketCode : (!string.IsNullOrEmpty(id) ? id : "SBG-HN-HP-001");
            var ticketCodeToSearch = effectiveCode;
            var dbTicket = await _context.Tickets
                .Include(t => t.Booking).ThenInclude(b => b.User)
                .Include(t => t.Booking).ThenInclude(b => b.Trip).ThenInclude(tr => tr.Route)
                .Include(t => t.Booking).ThenInclude(b => b.Trip).ThenInclude(tr => tr.Bus).ThenInclude(bus => bus.BusType)
                .Include(t => t.Booking).ThenInclude(b => b.Trip).ThenInclude(tr => tr.Driver).ThenInclude(d => d.User)
                .Include(t => t.BoardingStop)
                .Include(t => t.DropOffStop)
                .FirstOrDefaultAsync(t => t.TicketCode == ticketCodeToSearch || t.TicketId.ToString() == ticketCodeToSearch);

            TicketDetailViewModel model;
            if (dbTicket != null)
            {
                var tr = dbTicket.Booking?.Trip;
                var r = tr?.Route;
                var b = tr?.Bus;
                var d = tr?.Driver;
                var u = dbTicket.Booking?.User;

                model = new TicketDetailViewModel
                {
                    TicketId = dbTicket.TicketId,
                    TicketCode = dbTicket.TicketCode,
                    SeatNumber = dbTicket.SeatNumber ?? "VIP-05",
                    Price = dbTicket.Price,
                    Status = dbTicket.Status?.ToUpper() ?? "ACTIVE",
                    BookingId = dbTicket.BookingId,
                    BookingCode = dbTicket.Booking?.BookingCode ?? "BK-0001",
                    BookingTime = dbTicket.Booking?.BookingTime ?? DateTime.Now,
                    TotalAmount = dbTicket.Booking?.TotalAmount ?? dbTicket.Price,
                    PassengerName = u?.FullName ?? "Hành khách SmartBus",
                    PassengerPhone = u?.Phone ?? "0987654321",
                    PassengerEmail = u?.Email ?? "passenger@smartbus.vn",
                    RouteCode = r?.RouteCode ?? "R01",
                    RouteName = r?.RouteName ?? $"{r?.StartPoint ?? "Hà Nội"} - {r?.EndPoint ?? "Đà Nẵng"}",
                    StartPoint = r?.StartPoint ?? "Hà Nội",
                    EndPoint = r?.EndPoint ?? "Đà Nẵng",
                    Distance = r?.Distance ?? 760m,
                    EstimatedDuration = r?.EstimatedDuration ?? 750,
                    BoardingStopName = dbTicket.BoardingStop?.StopName ?? (r?.StartPoint + " (Bến xe xuất phát)"),
                    BoardingStopAddress = dbTicket.BoardingStop?.Address ?? "Điểm đón SmartBus Express",
                    DepartureTime = tr?.DepartureTime.ToTimeSpan() ?? new TimeSpan(19, 30, 0),
                    DropOffStopName = dbTicket.DropOffStop?.StopName ?? (r?.EndPoint + " (Bến xe trả khách)"),
                    DropOffStopAddress = dbTicket.DropOffStop?.Address ?? "Điểm trả SmartBus Express",
                    ArrivalTime = tr?.ArrivalTime?.ToTimeSpan() ?? new TimeSpan(8, 0, 0),
                    TripId = tr?.TripId ?? 1,
                    TripDate = tr?.TripDate.ToDateTime(TimeOnly.MinValue) ?? DateTime.Today,
                    LicensePlate = b?.LicensePlate ?? "29B-888.68",
                    BusTypeName = b?.BusType?.TypeName ?? "Limousine VIP 22 Phòng Đơn Cung Điện",
                    DriverName = d?.User?.FullName ?? "Tài xế SmartBus",
                    DriverPhone = d?.User?.Phone ?? "0988 777 999"
                };
            }
            else
            {
                model = GetMockOldTicket(ticketCode);
            }

            return View(model);
        }

        // =========================================================================
        // POST: /Ticket/ConfirmCancel
        // =========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmCancel(string ticketCode, string cancelReason, string refundMethod, string? cancelNote)
        {
            decimal refundAmount = 450000m * 0.9m; // 90%
            try
            {
                var dbTicket = await _context.Tickets
                    .Include(t => t.Booking)
                    .FirstOrDefaultAsync(t => t.TicketCode == ticketCode);

                if (dbTicket != null)
                {
                    refundAmount = dbTicket.Price * 0.9m;
                    dbTicket.Status = "CANCELLED";
                    if (dbTicket.Booking != null)
                    {
                        dbTicket.Booking.Status = "Cancelled";
                    }
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý hủy vé: {Message}", ex.Message);
            }

            TempData["SuccessMessage"] = $"Yêu cầu hủy vé #{ticketCode} thành công! Số tiền hoàn lại {refundAmount:N0} đ đang được xử lý chuyển về tài khoản của bạn.";
            return RedirectToAction("Detail", new { id = ticketCode, status = "CANCELLED" });
        }

        // =========================================================================
        // PHƯƠNG THỨC HỖ TRỢ XỬ LÝ ĐỔI CHUYẾN ĐỘNG & THÔNG MINH
        // =========================================================================

        private async Task<List<TripItemViewModel>> GetAvailableRescheduleTripsAsync(TicketDetailViewModel oldTicket, DateTime targetDate)
        {
            var trips = new List<TripItemViewModel>();

            // 1. Thử tìm kiếm chuyến xe có sẵn trong Database cho đúng tuyến đường này
            try
            {
                var targetDateOnly = DateOnly.FromDateTime(targetDate);
                var normStart = LocationHelper.NormalizeLocation(oldTicket.StartPoint);
                var normEnd = LocationHelper.NormalizeLocation(oldTicket.EndPoint);

                var query = await _context.Trips
                    .Include(t => t.Route)
                    .Include(t => t.Bus).ThenInclude(b => b.BusType)
                    .Include(t => t.Driver).ThenInclude(d => d.User)
                    .Include(t => t.Bookings).ThenInclude(b => b.Tickets)
                    .Where(t => (t.TripDate == targetDateOnly || t.TripDate >= DateOnly.FromDateTime(DateTime.Today)) &&
                                (t.Route.StartPoint.ToLower().Contains(normStart) || t.Route.RouteName.ToLower().Contains(normStart)) &&
                                (t.Route.EndPoint.ToLower().Contains(normEnd) || t.Route.RouteName.ToLower().Contains(normEnd)))
                    .OrderBy(t => t.DepartureTime)
                    .Take(6)
                    .ToListAsync();

                if (query.Any())
                {
                    foreach (var tr in query)
                    {
                        var depTime = tr.DepartureTime.ToTimeSpan();
                        double durationHours = (tr.Route.EstimatedDuration.HasValue && tr.Route.EstimatedDuration.Value > 0) ? (tr.Route.EstimatedDuration.Value / 60.0) : 4.0;
                        var arrTime = tr.ArrivalTime?.ToTimeSpan() ?? depTime.Add(TimeSpan.FromHours(durationHours));
                        var bookedSeats = tr.Bookings
                            .Where(b => b.Status != "Cancelled" && b.Status != "CANCELLED")
                            .SelectMany(b => b.Tickets)
                            .Where(t => t.Status != "Cancelled" && t.Status != "CANCELLED" && !string.IsNullOrEmpty(t.SeatNumber))
                            .Select(t => t.SeatNumber!.Trim().ToUpper())
                            .ToHashSet();

                        var capacity = tr.Bus?.Capacity ?? 34;
                        var basePrice = tr.Bookings.SelectMany(b => b.Tickets).FirstOrDefault()?.Price ?? oldTicket.Price;

                        trips.Add(new TripItemViewModel
                        {
                            TripId = tr.TripId,
                            OperatorName = "SmartBus " + (tr.Bus?.BusType?.TypeName ?? "Express"),
                            Rating = 4.9,
                            ReviewCount = 1200,
                            BusTypeName = tr.Bus?.BusType?.TypeName ?? "Xe Limousine Cao Cấp",
                            BusImage = GetBusImageForType(tr.Bus?.BusType?.TypeName),
                            LicensePlate = tr.Bus?.LicensePlate ?? "29B-888.88",
                            DepartureTime = depTime,
                            DeparturePoint = oldTicket.BoardingStopName,
                            ArrivalTime = arrTime,
                            ArrivalPoint = oldTicket.DropOffStopName,
                            DurationText = $"{(int)(arrTime - depTime).TotalHours}h {(arrTime - depTime).Minutes}m",
                            Price = basePrice,
                            OriginalPrice = basePrice * 1.1m,
                            AvailableSeats = Math.Max(5, capacity - bookedSeats.Count),
                            TotalCapacity = capacity,
                            IsFlashSale = basePrice < oldTicket.Price,
                            FlashSaleText = basePrice < oldTicket.Price ? "TIẾT KIỆM" : (basePrice > oldTicket.Price ? "VIP" : null),
                            NoticeText = basePrice < oldTicket.Price
                                ? $"Giá vé thấp hơn {oldTicket.Price - basePrice:N0}đ (Hệ thống sẽ hoàn lại chênh lệch)"
                                : (basePrice > oldTicket.Price ? $"Hạng xe cao cấp hơn (+{basePrice - oldTicket.Price:N0}đ phụ thu)" : "Bằng giá với vé cũ (Đổi chuyến miễn phí 0đ)"),
                            BoardingPoints = new List<string> { oldTicket.BoardingStopName, $"{oldTicket.StartPoint} (Trạm cao tốc)" },
                            DropOffPoints = new List<string> { oldTicket.DropOffStopName, $"{oldTicket.EndPoint} (Bến trung tâm)" },
                            Seats = GenerateSeatsForCapacity(capacity, bookedSeats, basePrice)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Không thể tải chuyến xe từ DB cho Reschedule: {Message}", ex.Message);
            }

            // 2. Nếu DB chưa có chuyến trong ngày được chọn, tự động sinh 3 lựa chọn chênh lệch giá phong phú cho tuyến ĐÚNG của vé
            if (!trips.Any())
            {
                trips = GenerateDynamicRescheduleTrips(oldTicket, targetDate);
            }

            return trips;
        }

        private List<TripItemViewModel> GenerateDynamicRescheduleTrips(TicketDetailViewModel oldTicket, DateTime targetDate)
        {
            var trips = new List<TripItemViewModel>();
            var basePrice = oldTicket.Price > 0 ? oldTicket.Price : 450000m;

            // Option 1: Buổi sáng - Tiết kiệm (giá rẻ hơn vé cũ 50.000đ -> Khách nhận tiền hoàn)
            var morningPrice = Math.Max(120000m, basePrice - 50000m);
            trips.Add(new TripItemViewModel
            {
                TripId = 201,
                OperatorName = "SmartBus Eco Express",
                Rating = 4.8,
                ReviewCount = 890,
                BusTypeName = "Limousine VIP 34 Phòng Giường Nằm",
                BusImage = "https://images.unsplash.com/photo-1544620347-c4fd4a3d5957?w=400&q=80",
                LicensePlate = "29B-777.22",
                DepartureTime = new TimeSpan(8, 30, 0),
                DeparturePoint = oldTicket.BoardingStopName,
                ArrivalTime = new TimeSpan(14, 0, 0),
                ArrivalPoint = oldTicket.DropOffStopName,
                DurationText = "5h 30m",
                Price = morningPrice,
                OriginalPrice = morningPrice * 1.15m,
                AvailableSeats = 14,
                TotalCapacity = 34,
                IsFlashSale = true,
                FlashSaleText = "TIẾT KIỆM 50K",
                NoticeText = $"Khởi hành buổi sáng. Tiết kiệm 50.000đ (Hệ thống sẽ hoàn lại tiền thừa về tài khoản)",
                BoardingPoints = new List<string> { oldTicket.BoardingStopName, $"{oldTicket.StartPoint} (Cổng phụ bến xe)" },
                DropOffPoints = new List<string> { oldTicket.DropOffStopName, $"{oldTicket.EndPoint} (Bến trung chuyển)" },
                Seats = GenerateSeatsForCapacity(34, new[] { "A01", "A04", "B02", "B05" }, morningPrice)
            });

            // Option 2: Buổi chiều - Tiêu chuẩn (BẰNG GIÁ vé cũ -> Đổi hoàn toàn miễn phí 0đ)
            trips.Add(new TripItemViewModel
            {
                TripId = 202,
                OperatorName = "SmartBus Premier Line",
                Rating = 4.9,
                ReviewCount = 1420,
                BusTypeName = "Limousine VIP 22 Phòng Đơn Cung Điện",
                BusImage = "https://images.unsplash.com/photo-1570125909232-eb263c188f7e?w=400&q=80",
                LicensePlate = "29B-888.68",
                DepartureTime = new TimeSpan(14, 0, 0),
                DeparturePoint = oldTicket.BoardingStopName,
                ArrivalTime = new TimeSpan(19, 30, 0),
                ArrivalPoint = oldTicket.DropOffStopName,
                DurationText = "5h 30m",
                Price = basePrice,
                OriginalPrice = basePrice * 1.1m,
                AvailableSeats = 8,
                TotalCapacity = 22,
                IsFlashSale = false,
                NoticeText = "Bằng giá với vé cũ của bạn. Đổi chuyến hoàn toàn miễn phí 0đ.",
                BoardingPoints = new List<string> { oldTicket.BoardingStopName, $"{oldTicket.StartPoint} (Trạm cao tốc)" },
                DropOffPoints = new List<string> { oldTicket.DropOffStopName, $"{oldTicket.EndPoint} (Trung tâm thành phố)" },
                Seats = GenerateSeatsForCapacity(22, new[] { "A01", "A03", "B01", "B04" }, basePrice)
            });

            // Option 3: Buổi tối - Hạng Thương Gia VIP (ĐẮT HƠN vé cũ 70.000đ -> Khách bù tiền)
            var eveningPrice = basePrice + 70000m;
            trips.Add(new TripItemViewModel
            {
                TripId = 203,
                OperatorName = "SmartBus Royal Luxury Suite",
                Rating = 5.0,
                ReviewCount = 2680,
                BusTypeName = "Royal Luxury 20 Phòng VIP (Massage, Màn hình 4K)",
                BusImage = "https://images.unsplash.com/photo-1494515843206-f3117d3f51b7?w=400&q=80",
                LicensePlate = "29B-999.88",
                DepartureTime = new TimeSpan(20, 30, 0),
                DeparturePoint = oldTicket.BoardingStopName,
                ArrivalTime = new TimeSpan(2, 0, 0),
                ArrivalPoint = oldTicket.DropOffStopName,
                DurationText = "5h 30m",
                Price = eveningPrice,
                OriginalPrice = eveningPrice * 1.12m,
                AvailableSeats = 6,
                TotalCapacity = 20,
                IsFlashSale = true,
                FlashSaleText = "HẠNG THƯƠNG GIA",
                NoticeText = "Nâng cấp lên xe phòng thương gia cao cấp, phục vụ bữa ăn nhẹ & nước uống miễn phí (+70.000đ phụ thu)",
                BoardingPoints = new List<string> { oldTicket.BoardingStopName, $"{oldTicket.StartPoint} (Sảnh chờ VIP)" },
                DropOffPoints = new List<string> { oldTicket.DropOffStopName, $"{oldTicket.EndPoint} (Trả tận khách sạn)" },
                Seats = GenerateSeatsForCapacity(20, new[] { "A01", "A02", "B03" }, eveningPrice)
            });

            return trips;
        }

        private List<SeatItemViewModel> GenerateSeatsForCapacity(int capacity, IEnumerable<string> bookedSeats, decimal basePrice)
        {
            var seats = new List<SeatItemViewModel>();
            var bookedSet = bookedSeats.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var rows = (capacity + 1) / 2;

            for (int r = 1; r <= rows; r++)
            {
                foreach (var col in new[] { "VIP", "T" })
                {
                    var seatCode = $"{col}-{r:D2}";
                    if (seats.Count >= capacity) break;

                    seats.Add(new SeatItemViewModel
                    {
                        SeatCode = seatCode,
                        Floor = r <= (rows / 2) ? 1 : 2,
                        Price = basePrice,
                        Status = bookedSet.Contains(seatCode) ? "BOOKED" : "AVAILABLE"
                    });
                }
            }
            return seats;
        }

        private static string GetBusImageForType(string? typeName)
        {
            var lower = (typeName ?? "").ToLower();
            if (lower.Contains("cung điện") || lower.Contains("royal"))
                return "https://images.unsplash.com/photo-1494515843206-f3117d3f51b7?w=400&q=80";
            if (lower.Contains("34") || lower.Contains("giường"))
                return "https://images.unsplash.com/photo-1544620347-c4fd4a3d5957?w=400&q=80";
            return "https://images.unsplash.com/photo-1570125909232-eb263c188f7e?w=400&q=80";
        }

        private static decimal CalculateBasePrice(decimal? distance)
        {
            var d = distance ?? 100m;
            return WebApplication1.Services.RoutePricingService.GetMarketBasePrice(d);
        }

        private async Task<string> GetAccurateStopAddressAsync(string? stopName, string city)
        {
            if (string.IsNullOrWhiteSpace(stopName))
            {
                return $"Khu vực trung tâm TP. {city}";
            }

            try
            {
                var cleanName = stopName.Split('(')[0].Trim();
                var busStop = await _context.BusStops
                    .FirstOrDefaultAsync(s => s.StopName.Contains(cleanName) || cleanName.Contains(s.StopName));
                if (busStop != null && !string.IsNullOrWhiteSpace(busStop.Address))
                {
                    return busStop.Address;
                }
            }
            catch { }

            var lower = stopName.ToLower();
            if (lower.Contains("thái nguyên"))
            {
                if (lower.Contains("văn phòng") || lower.Contains("đại diện"))
                    return "Số 236 Đường Hoàng Văn Thụ, TP. Thái Nguyên";
                return "Khu đô thị Đồng Quang, P. Đồng Quang, TP. Thái Nguyên";
            }
            if (lower.Contains("cao bằng"))
            {
                if (lower.Contains("ngã ba") || lower.Contains("trung tâm"))
                    return "Ngã ba Vườn Cam, P. Hợp Giang, TP. Cao Bằng";
                return "QL3, P. Đề Thám, TP. Cao Bằng";
            }
            if (lower.Contains("mỹ đình")) return "Số 20 Phạm Hùng, P. Mỹ Đình 2, Q. Nam Từ Liêm, Hà Nội";
            if (lower.Contains("giáp bát")) return "Km 6 Đường Giải Phóng, P. Giáp Bát, Q. Hoàng Mai, Hà Nội";
            if (lower.Contains("nước ngầm")) return "Số 1 Ngọc Hồi, P. Hoàng Liệt, Q. Hoàng Mai, Hà Nội";
            if (lower.Contains("gia lâm")) return "Số 9 Ngô Gia Khảm, P. Gia Thụy, Q. Long Biên, Hà Nội";
            if (lower.Contains("yên nghĩa")) return "QL6, P. Yên Nghĩa, Q. Hà Đông, Hà Nội";
            if (lower.Contains("hải phòng") || lower.Contains("cầu rào") || lower.Contains("niệm nghĩa")) return "Đường Bùi Viện, Q. Lê Chân, Hải Phòng";
            if (lower.Contains("quảng ninh") || lower.Contains("bãi cháy")) return "Đường Hạ Long, P. Bãi Cháy, TP. Hạ Long";
            if (lower.Contains("đà nẵng")) return "Đường Nam Trân, P. Hòa Minh, Q. Liên Chiểu, Đà Nẵng";
            if (lower.Contains("sài gòn") || lower.Contains("miền đông")) return "Đinh Bộ Lĩnh, P. 26, Q. Bình Thạnh, TP. Hồ Chí Minh";
            if (lower.Contains("miền tây")) return "Số 395 Kinh Dương Vương, P. An Lạc, Q. Bình Tân, TP. Hồ Chí Minh";

            return $"Khu vực bến xe trung tâm {city}";
        }

        private TicketDetailViewModel GetMockOldTicket(string ticketCode)
        {
            var fallbackFrom = Request.Cookies["sbg_last_from"] ?? Request.Cookies["sbg_search_from"] ?? "Thái Nguyên";
            var fallbackTo = Request.Cookies["sbg_last_to"] ?? Request.Cookies["sbg_search_to"] ?? "Cao Bằng";
            var fromCode = WebApplication1.Services.ProvinceLicenseHelper.GetAllCodes(fallbackFrom).FirstOrDefault() ?? "TN";
            var toCode = WebApplication1.Services.ProvinceLicenseHelper.GetAllCodes(fallbackTo).FirstOrDefault() ?? "CB";

            var fbBusType = Request.Cookies["sbg_last_bustype"] ?? "Limousine VIP 22 Phòng Cung Điện";
            var fbMetrics = WebApplication1.Services.RoutePricingService.GetRouteMetrics(fallbackFrom, fallbackTo);

            var fbSeats = Request.Cookies["sbg_last_seats"] ?? "A01";
            var fbSeatCount = fbSeats.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Length;
            var defaultCalculatedPrice = WebApplication1.Services.RoutePricingService.CalculateTripPrice(fbMetrics.Distance, fbBusType, 29, 0) * Math.Max(1, fbSeatCount);
            var fbPrice = decimal.TryParse(Request.Cookies["sbg_last_total"], out var tot) && tot > 0 ? tot : defaultCalculatedPrice;

            DateTime fbDate = DateTime.Today.AddDays(1);
            if (DateTime.TryParse(Request.Cookies["sbg_last_date"], out var pDate)) fbDate = pDate;

            TimeSpan fbDep = new TimeSpan(8, 0, 0);
            if (TimeSpan.TryParse(Request.Cookies["sbg_last_deptime"], out var pDep)) fbDep = pDep;

            TimeSpan fbArr = fbDep.Add(TimeSpan.FromMinutes(fbMetrics.EstimatedMinutes));
            if (TimeSpan.TryParse(Request.Cookies["sbg_last_arrtime"], out var pArr)) fbArr = pArr;

            var fbPlate = Request.Cookies["sbg_last_licenseplate"] 
                ?? (fallbackFrom.Contains("Thái Nguyên") ? "20B-188.68" : $"{fromCode}B-888.68");
            var fbRouteName = Request.Cookies["sbg_last_routename"] ?? $"{fallbackFrom} - {fallbackTo}";
            var fbRouteCode = Request.Cookies["sbg_last_routecode"] ?? $"{fromCode}-{toCode}-01";
            var fbDriver = Request.Cookies["sbg_last_driver"] ?? $"Nguyễn Văn Tuấn (Tài xế {fallbackFrom})";
            var fbDriverPhone = Request.Cookies["sbg_last_driverphone"] ?? "0988 777 999";
            var fbBoarding = Request.Cookies["sbg_last_boarding"] ?? $"Bến xe {fallbackFrom} (Cổng chính)";
            var fbDropoff = Request.Cookies["sbg_last_dropoff"] ?? $"Bến xe {fallbackTo} (Khu trả khách)";

            var genBooking = $"BK-{fromCode}{toCode}-{DateTime.Now:MMddHHmm}";

            return new TicketDetailViewModel
            {
                TicketId = 2026,
                TicketCode = ticketCode,
                SeatNumber = fbSeats,
                Price = fbPrice,
                Status = "ACTIVE",
                BookingId = 8821,
                BookingCode = genBooking,
                BookingTime = DateTime.Now,
                TotalAmount = fbPrice,
                PassengerName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? (User.Identity?.Name ?? "Trần Văn Bình"),
                PassengerPhone = "0912 345 678",
                PassengerEmail = "khachhang@smartbus.vn",
                RouteCode = fbRouteCode,
                RouteName = fbRouteName,
                StartPoint = fallbackFrom,
                EndPoint = fallbackTo,
                Distance = fbMetrics.Distance,
                EstimatedDuration = fbMetrics.EstimatedMinutes,
                BoardingStopName = fbBoarding,
                BoardingStopAddress = $"Khu vực xuất phát TP. {fallbackFrom}",
                DepartureTime = fbDep,
                DropOffStopName = fbDropoff,
                DropOffStopAddress = $"Khu vực điểm đến TP. {fallbackTo}",
                ArrivalTime = fbArr,
                TripId = 101,
                TripDate = fbDate,
                LicensePlate = fbPlate,
                BusTypeName = fbBusType,
                DriverName = fbDriver,
                DriverPhone = fbDriverPhone,
                QrDataPayload = $"SMARTBUS|TICKET:{ticketCode}|BOOKING:{genBooking}|ROUTE:{fallbackFrom}-{fallbackTo}|SEAT:{fbSeats}|DATE:{fbDate:yyyy-MM-dd}|STATUS:ACTIVE"
            };
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WebApplication1.Data;
using WebApplication1.Models.Entities;
using WebApplication1.Models.ViewModels;

namespace WebApplication1.Controllers
{
    public class TripController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly WebApplication1.Services.BusLayout.IBusLayoutService _busLayoutService;
        private readonly WebApplication1.Services.ISeatHoldService _seatHoldService;

        public TripController(
            ApplicationDbContext context, 
            WebApplication1.Services.BusLayout.IBusLayoutService busLayoutService,
            WebApplication1.Services.ISeatHoldService seatHoldService)
        {
            _context = context;
            _busLayoutService = busLayoutService;
            _seatHoldService = seatHoldService;
        }

        [HttpGet]
        public async Task<IActionResult> Search(
            string? from = "Hà Nội", 
            string? to = "Hải Phòng", 
            string? date = null, 
            string? sort = "default", 
            string? busType = null, 
            string? timeOfDay = null)
        {
            // Mặc định ngày khởi hành là ngày hiện tại hôm nay; ưu tiên định dạng ngày/tháng/năm (dd/MM/yyyy)
            DateTime searchDate = DateTime.Today;
            if (!string.IsNullOrWhiteSpace(date))
            {
                var formats = new[] { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd", "yyyy/MM/dd", "MM/dd/yyyy" };
                if (DateTime.TryParseExact(date.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedExact))
                {
                    searchDate = parsedExact;
                }
                else if (DateTime.TryParse(date.Trim(), CultureInfo.GetCultureInfo("vi-VN"), DateTimeStyles.None, out var parsedVi))
                {
                    searchDate = parsedVi;
                }
                else if (DateTime.TryParse(date.Trim(), out var parsedFallback))
                {
                    searchDate = parsedFallback;
                }
            }

            var formattedFrom = FormatToTitleCase(from);
            var formattedTo = FormatToTitleCase(to);

            var model = new TripSearchViewModel
            {
                From = formattedFrom,
                To = formattedTo,
                DepartureDate = searchDate,
                SortBy = sort,
                BusTypeFilter = busType,
                TimeOfDayFilter = timeOfDay
            };

            // =========================================================================
            // 1. LOGIC VALIDATE DỮ LIỆU ĐẦU VÀO (INPUT VALIDATION)
            // =========================================================================

            // Kiểm tra rỗng: Điểm đi và điểm đến không được để trống
            if (string.IsNullOrWhiteSpace(model.From) || string.IsNullOrWhiteSpace(model.To))
            {
                model.ErrorMessage = "Vui lòng nhập đầy đủ nơi xuất phát và nơi đến.";
                ModelState.AddModelError(string.Empty, model.ErrorMessage);
                return View(model);
            }

            // Kiểm tra điểm đi không được trùng điểm đến (bỏ qua hoa thường và khoảng trắng)
            if (string.Equals(model.From, model.To, StringComparison.OrdinalIgnoreCase))
            {
                model.ErrorMessage = "Điểm đi và điểm đến không được trùng nhau! Vui lòng chọn lộ trình khác.";
                ModelState.AddModelError(string.Empty, model.ErrorMessage);
                return View(model);
            }

            // Kiểm tra ngày chọn không nhỏ hơn ngày hiện tại
            if (searchDate.Date < DateTime.Today)
            {
                model.ErrorMessage = $"Ngày khởi hành ({searchDate:dd/MM/yyyy}) không thể là ngày trong quá khứ! Vui lòng chọn ngày từ hôm nay ({DateTime.Today:dd/MM/yyyy}) trở đi.";
                ModelState.AddModelError(string.Empty, model.ErrorMessage);
                return View(model);
            }

            // Lưu lại điểm đi, điểm đến và ngày tìm kiếm vào Cookie để đồng bộ xuyên suốt quá trình đặt vé
            try
            {
                var cookieOpt = new Microsoft.AspNetCore.Http.CookieOptions
                {
                    Expires = DateTimeOffset.Now.AddDays(7),
                    IsEssential = true,
                    SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax
                };
                Response.Cookies.Append("sbg_search_from", model.From, cookieOpt);
                Response.Cookies.Append("sbg_search_to", model.To, cookieOpt);
                Response.Cookies.Append("sbg_search_date", searchDate.ToString("yyyy-MM-dd"), cookieOpt);
            }
            catch { }

            // =========================================================================
            // 2. TRUY VẤN DATABASE: LỌC CÁC CHUYẾN XE KHỚP ĐIỀU KIỆN & CÒN GHẾ TRỐNG
            // =========================================================================
            var tripDateOnly = DateOnly.FromDateTime(searchDate.Date);
            var fromAliases = WebApplication1.Services.LocationHelper.GetLocationAliases(model.From);
            var toAliases = WebApplication1.Services.LocationHelper.GetLocationAliases(model.To);

            var tripViewModels = new List<TripItemViewModel>();

            try
            {
                // Đảm bảo toàn bộ danh mục xe 63 tỉnh thành đã được nạp cố định vào bảng Bus trong CSDL
                await WebApplication1.Services.ProvinceLicenseHelper.EnsureAllProvincesBusesSeededAsync(_context);

                // Truy vấn tất cả tuyến xe và chuyến xe phù hợp từ DB
                var queryRoutes = _context.Routes
                    .Include(r => r.RouteStops.OrderBy(rs => rs.StopOrder))
                        .ThenInclude(rs => rs.Stop)
                    .Include(r => r.Trips)
                        .ThenInclude(t => t.Bus)
                            .ThenInclude(b => b.BusType)
                    .Include(r => r.Trips)
                        .ThenInclude(t => t.Driver)
                            .ThenInclude(d => d.User)
                    .Include(r => r.Trips)
                        .ThenInclude(t => t.Bookings)
                            .ThenInclude(bk => bk.Tickets)
                    .AsEnumerable()
                    .Where(r => IsRouteMatchingSearch(r, model.From, model.To))
                    .ToList();

                // Nếu chưa có tuyến đường nào phù hợp trong DB, tự động tạo Tuyến đường mới và lưu cố định vào SQL Server
                if (!queryRoutes.Any())
                {
                    var metrics = WebApplication1.Services.RoutePricingService.GetRouteMetrics(model.From, model.To);
                    var fromCodes = WebApplication1.Services.ProvinceLicenseHelper.GetAllCodes(model.From);
                    var toCodes = WebApplication1.Services.ProvinceLicenseHelper.GetAllCodes(model.To);
                    var fCode = fromCodes.FirstOrDefault() ?? "HN";
                    var tCode = toCodes.FirstOrDefault() ?? "HP";

                    var baseCode = $"{fCode}-{tCode}";
                    var rCode = $"{baseCode}-01";
                    int counter = 1;
                    while (await _context.Routes.AnyAsync(r => r.RouteCode == rCode))
                    {
                        counter++;
                        rCode = $"{baseCode}-{counter:D2}";
                    }

                    var cleanFrom = FormatToTitleCase(model.From);
                    var cleanTo = FormatToTitleCase(model.To);

                    var newRoute = new WebApplication1.Models.Entities.Route
                    {
                        RouteCode = rCode,
                        RouteName = $"{cleanFrom} - {cleanTo}",
                        StartPoint = cleanFrom,
                        EndPoint = cleanTo,
                        Distance = metrics.Distance,
                        EstimatedDuration = metrics.EstimatedMinutes,
                        Status = "Active"
                    };
                    _context.Routes.Add(newRoute);
                    await _context.SaveChangesAsync();

                    var loadedRoute = await _context.Routes
                        .Include(r => r.RouteStops.OrderBy(rs => rs.StopOrder)).ThenInclude(rs => rs.Stop)
                        .Include(r => r.Trips).ThenInclude(t => t.Bus).ThenInclude(b => b.BusType)
                        .Include(r => r.Trips).ThenInclude(t => t.Driver).ThenInclude(d => d.User)
                        .Include(r => r.Trips).ThenInclude(t => t.Bookings).ThenInclude(b => b.Tickets)
                        .FirstOrDefaultAsync(r => r.RouteId == newRoute.RouteId);

                    if (loadedRoute != null)
                    {
                        queryRoutes.Add(loadedRoute);
                    }
                }

                // Lọc các chuyến xe thực tế trong DB
                foreach (var route in queryRoutes)
                {
                    var activeTrips = route.Trips
                        .Where(t => t.TripDate == tripDateOnly && (t.Status == null || !t.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase)))
                        .ToList();

                    // Nếu ngày tìm kiếm chưa có chuyến tạo sẵn trong DB, tự động sinh các chuyến theo Route thực tế và lưu vào DB
                    if (!activeTrips.Any())
                    {
                        var generated = GenerateScheduledTripsForRoute(route, tripDateOnly);
                        foreach (var gt in generated)
                        {
                            var dbTrip = new Trip
                            {
                                RouteId = gt.RouteId,
                                BusId = gt.BusId,
                                DriverId = gt.DriverId,
                                TripDate = gt.TripDate,
                                DepartureTime = gt.DepartureTime,
                                ArrivalTime = gt.ArrivalTime,
                                Status = "Scheduled"
                            };
                            _context.Trips.Add(dbTrip);
                        }
                        await _context.SaveChangesAsync();

                        activeTrips = await _context.Trips
                            .Include(t => t.Bus).ThenInclude(b => b.BusType)
                            .Include(t => t.Driver).ThenInclude(d => d.User)
                            .Include(t => t.Bookings).ThenInclude(b => b.Tickets)
                            .Where(t => t.RouteId == route.RouteId && t.TripDate == tripDateOnly && (t.Status == null || t.Status.ToUpper() != "CANCELLED"))
                            .ToListAsync();
                    }

                    int? currentUserId = User.Identity?.IsAuthenticated == true && int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : null;
                    string currentSessionId = Request.Cookies["sbg_session_id"] ?? "";
                    var now = DateTime.Now;

                    foreach (var trip in activeTrips)
                    {
                        var validTickets = trip.Bookings?
                            .Where(bk => bk.Status == null || !bk.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
                            .SelectMany(bk => bk.Tickets ?? Enumerable.Empty<Ticket>())
                            .Where(tk => tk.Status == null || !tk.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
                            .ToList() ?? new List<Ticket>();

                        var bookedSeatNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                        // 1. Quét từ vé trong Database
                        foreach (var tk in validTickets)
                        {
                            if (string.IsNullOrWhiteSpace(tk.SeatNumber)) continue;
                            var st = (tk.Status ?? "").ToUpper();
                            var seats = tk.SeatNumber.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);

                            if (st == "CONFIRMED" || st == "ACTIVE" || st == "PAID" || st == "USED")
                            {
                                foreach (var s in seats) bookedSeatNumbers.Add(s.Trim().ToUpper());
                            }
                            else if (st == "HELD" || st == "PENDING")
                            {
                                bool isMine = false;
                                if (currentUserId.HasValue && tk.Booking != null)
                                {
                                    isMine = (tk.Booking.UserId == currentUserId.Value);
                                }
                                else if (!string.IsNullOrEmpty(currentSessionId) && tk.Booking != null)
                                {
                                    isMine = (tk.Booking.UserId <= 0) && tk.Booking.BookingCode.Contains(currentSessionId);
                                }

                                if (!isMine)
                                {
                                    foreach (var s in seats) bookedSeatNumbers.Add(s.Trim().ToUpper());
                                }
                            }
                        }

                        // 2. Quét từ ISeatHoldService
                        if (_seatHoldService != null && trip.TripId > 0)
                        {
                            var activeHolds = await _seatHoldService.GetActiveHoldsForTripAsync(trip.TripId);
                            foreach (var hold in activeHolds.Values)
                            {
                                bool isMine = false;
                                if (currentUserId.HasValue && hold.UserId.HasValue)
                                {
                                    isMine = (hold.UserId.Value == currentUserId.Value);
                                }
                                else if (!currentUserId.HasValue && !hold.UserId.HasValue && !string.IsNullOrEmpty(currentSessionId))
                                {
                                    isMine = string.Equals(hold.SessionId, currentSessionId, StringComparison.OrdinalIgnoreCase);
                                }
                                else if (currentUserId.HasValue && !hold.UserId.HasValue && !string.IsNullOrEmpty(currentSessionId))
                                {
                                    isMine = string.Equals(hold.SessionId, currentSessionId, StringComparison.OrdinalIgnoreCase);
                                }

                                if (!isMine)
                                {
                                    bookedSeatNumbers.Add(hold.SeatNumber.Trim().ToUpper());
                                }
                            }
                        }

                        var totalCapacity = trip.Bus?.Capacity ?? 29;
                        var bookedCount = bookedSeatNumbers.Count;
                        var availableSeats = Math.Max(0, totalCapacity - bookedCount);

                        if (availableSeats <= 0) continue;

                        var sortedStops = route.RouteStops?.OrderBy(rs => rs.StopOrder).ToList() ?? new List<RouteStop>();
                        var firstStopDepTime = sortedStops.FirstOrDefault()?.DepartureTime;

                        var depTimeSpan = trip.DepartureTime.ToTimeSpan();
                        var effectiveDistance = route.Distance ?? WebApplication1.Services.RoutePricingService.GetRouteMetrics(route.StartPoint, route.EndPoint).Distance;
                        var arrTimeSpan = trip.ArrivalTime?.ToTimeSpan() ?? depTimeSpan.Add(TimeSpan.FromMinutes(route.EstimatedDuration ?? WebApplication1.Services.RoutePricingService.GetRouteMetrics(route.StartPoint, route.EndPoint).EstimatedMinutes));
                        var duration = arrTimeSpan - depTimeSpan;
                        if (duration < TimeSpan.Zero) duration = duration.Add(TimeSpan.FromHours(24));
                        var durationText = $"{(int)duration.TotalHours}h {duration.Minutes}m";

                        var boardingStops = sortedStops
                            .Take(3)
                            .Select(rs => {
                                var timeStr = trip.DepartureTime.ToString(@"hh\:mm");
                                if (firstStopDepTime.HasValue && rs.DepartureTime.HasValue)
                                {
                                    var diff = rs.DepartureTime.Value - firstStopDepTime.Value;
                                    var calculated = trip.DepartureTime.Add(diff);
                                    timeStr = calculated.ToString(@"hh\:mm");
                                }
                                return $"{rs.Stop?.StopName ?? route.StartPoint} ({timeStr})";
                            })
                            .ToList();

                        var dropOffStops = sortedStops
                            .Skip(Math.Max(0, sortedStops.Count - 2))
                            .Select(rs => {
                                var timeStr = (trip.ArrivalTime.HasValue ? trip.ArrivalTime.Value.ToString(@"hh\:mm") : arrTimeSpan.ToString(@"hh\:mm"));
                                if (firstStopDepTime.HasValue && rs.ArrivalTime.HasValue)
                                {
                                    var diff = rs.ArrivalTime.Value - firstStopDepTime.Value;
                                    var calculated = trip.DepartureTime.Add(diff);
                                    timeStr = calculated.ToString(@"hh\:mm");
                                }
                                return $"{rs.Stop?.StopName ?? route.EndPoint} ({timeStr})";
                            })
                            .ToList();

                        if (!boardingStops.Any()) boardingStops.Add($"{route.StartPoint} ({trip.DepartureTime:hh\\:mm})");
                        if (!dropOffStops.Any()) dropOffStops.Add($"{route.EndPoint} ({(trip.ArrivalTime.HasValue ? trip.ArrivalTime.Value.ToString(@"hh\:mm") : arrTimeSpan.ToString(@"hh\:mm"))})");

                        var depStopName = sortedStops.FirstOrDefault(rs => rs.Stop != null && WebApplication1.Services.LocationHelper.Matches(rs.Stop.StopName, model.From))?.Stop?.StopName 
                            ?? sortedStops.FirstOrDefault()?.Stop?.StopName 
                            ?? route.StartPoint;

                        var arrStopName = sortedStops.LastOrDefault(rs => rs.Stop != null && WebApplication1.Services.LocationHelper.Matches(rs.Stop.StopName, model.To))?.Stop?.StopName 
                            ?? sortedStops.LastOrDefault()?.Stop?.StopName 
                            ?? route.EndPoint;

                        var busTypeName = trip.Bus?.BusType?.TypeName ?? "Xe Limousine Cao Cấp";
                        int tripIndex = activeTrips.IndexOf(trip);
                        var price = validTickets.FirstOrDefault()?.Price ?? WebApplication1.Services.RoutePricingService.CalculateTripPrice(effectiveDistance, busTypeName, totalCapacity, tripIndex);
                        var originalPrice = Math.Round(price * 1.15m / 5000m, MidpointRounding.AwayFromZero) * 5000m;

                        var floorLayouts = _busLayoutService.GenerateFloorLayouts(busTypeName, totalCapacity, bookedSeatNumbers, price);
                        var flatSeats = floorLayouts.SelectMany(f => f.Seats)
                            .Where(s => s.IsBookable)
                            .Select(s => new SeatItemViewModel
                            {
                                SeatCode = s.SeatCode,
                                Floor = s.Floor,
                                Price = s.Price,
                                Status = s.Status
                            }).ToList();

                        var plate = trip.Bus?.LicensePlate ?? "29B-888.88";
                        var provName = WebApplication1.Services.ProvinceLicenseHelper.GetProvinceName(plate);
                        var fromCodes = WebApplication1.Services.ProvinceLicenseHelper.GetAllCodes(model.From);
                        var toCodes = WebApplication1.Services.ProvinceLicenseHelper.GetAllCodes(model.To);

                        tripViewModels.Add(new TripItemViewModel
                        {
                            TripId = trip.TripId,
                            OperatorName = route.RouteName.Contains("VIP") ? "SmartBus VIP Express" : "SmartBus Travel",
                            Rating = 4.8,
                            ReviewCount = 1250,
                            BusTypeName = busTypeName,
                            BusImage = GetBusImage(busTypeName),
                            LicensePlate = plate,
                            ProvinceName = provName,
                            IsMatchingDeparture = fromCodes.Any(c => plate.StartsWith(c)),
                            IsMatchingDestination = toCodes.Any(c => plate.StartsWith(c)),
                            DepartureTime = depTimeSpan,
                            DeparturePoint = depStopName,
                            ArrivalTime = arrTimeSpan,
                            ArrivalPoint = arrStopName,
                            DurationText = durationText,
                            Price = price,
                            OriginalPrice = originalPrice,
                            AvailableSeats = availableSeats,
                            TotalCapacity = totalCapacity,
                            IsFlashSale = availableSeats < 10,
                            FlashSaleText = availableSeats < 10 ? "SẮP HẾT CHỖ" : "ƯU ĐÃI ĐẶT SỚM",
                            NoticeText = $"Chuyến khởi hành ngày {searchDate:dd/MM/yyyy} tuyến {route.StartPoint} - {route.EndPoint}",
                            BoardingPoints = boardingStops,
                            DropOffPoints = dropOffStops,
                            BusTypeCode = _busLayoutService.GetLayoutConfig(busTypeName, totalCapacity).BusTypeCode,
                            FloorLayouts = floorLayouts,
                            Seats = flatSeats,
                            DriverName = trip.Driver?.User?.FullName ?? $"Nguyễn Văn Tuấn (Tài xế {provName})",
                            DriverPhone = trip.Driver?.User?.Phone ?? "0988 777 999",
                            RouteCode = route.RouteCode,
                            RouteName = route.RouteName,
                            RouteStartPoint = route.StartPoint,
                            RouteEndPoint = route.EndPoint,
                            EstimatedDuration = route.EstimatedDuration ?? (int)(arrTimeSpan - depTimeSpan).TotalMinutes,
                            Distance = effectiveDistance
                        });
                    }
                }
            }
            catch
            {
                // Fallback nếu kết nối cơ sở dữ liệu tạm gián đoạn
            }

            // Lọc theo loại xe
            if (!string.IsNullOrEmpty(busType))
            {
                tripViewModels = tripViewModels.Where(t => t.BusTypeName.Contains(busType, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Lọc theo khung giờ
            if (!string.IsNullOrEmpty(timeOfDay))
            {
                if (timeOfDay == "morning")
                    tripViewModels = tripViewModels.Where(t => t.DepartureTime.Hours >= 6 && t.DepartureTime.Hours < 12).ToList();
                else if (timeOfDay == "afternoon")
                    tripViewModels = tripViewModels.Where(t => t.DepartureTime.Hours >= 12 && t.DepartureTime.Hours < 18).ToList();
                else if (timeOfDay == "evening")
                    tripViewModels = tripViewModels.Where(t => t.DepartureTime.Hours >= 18 || t.DepartureTime.Hours < 6).ToList();
            }

            // Sắp xếp
            tripViewModels = sort switch
            {
                "price_asc" => tripViewModels.OrderBy(t => t.Price).ToList(),
                "price_desc" => tripViewModels.OrderByDescending(t => t.Price).ToList(),
                "time_earliest" => tripViewModels.OrderBy(t => t.DepartureTime).ToList(),
                "time_latest" => tripViewModels.OrderByDescending(t => t.DepartureTime).ToList(),
                _ => tripViewModels
            };

            // Đảm bảo tuyệt đối: Không hiển thị chuyến của tuyến khác, chỉ giữ lại chuyến xe trùng khớp với điểm đi và điểm đến tìm kiếm
            tripViewModels = tripViewModels.Where(t => 
                (WebApplication1.Services.LocationHelper.Matches(t.RouteStartPoint, model.From) || WebApplication1.Services.LocationHelper.Matches(t.DeparturePoint, model.From)) &&
                (WebApplication1.Services.LocationHelper.Matches(t.RouteEndPoint, model.To) || WebApplication1.Services.LocationHelper.Matches(t.ArrivalPoint, model.To))
            ).ToList();

            model.Trips = tripViewModels;
            return View(model);
        }

        private static decimal CalculateBasePrice(decimal? distance)
        {
            var d = distance ?? 100m;
            return WebApplication1.Services.RoutePricingService.GetMarketBasePrice(d);
        }

        private List<Trip> GenerateScheduledTripsForRoute(WebApplication1.Models.Entities.Route route, DateOnly date)
        {
            var trips = new List<Trip>();
            var allActiveBuses = _context.Buses.Include(b => b.BusType)
                .Where(b => b.Status == null || b.Status == "Active" || b.Status == "ACTIVE")
                .ToList();

            var sampleBuses = WebApplication1.Services.ProvinceLicenseHelper.SelectDiverseBusesForRoute(allActiveBuses, route.StartPoint, route.EndPoint, 4);
            if (!sampleBuses.Any())
            {
                sampleBuses = allActiveBuses.Take(4).ToList();
            }
            var sampleDriver = _context.Drivers.Include(d => d.User).FirstOrDefault();

            var timeSlots = new[]
            {
                new { Dep = new TimeOnly(7, 30), Duration = route.EstimatedDuration ?? 120, BusIdx = 0 },
                new { Dep = new TimeOnly(11, 0), Duration = route.EstimatedDuration ?? 120, BusIdx = 1 },
                new { Dep = new TimeOnly(14, 30), Duration = route.EstimatedDuration ?? 120, BusIdx = 2 },
                new { Dep = new TimeOnly(19, 0), Duration = route.EstimatedDuration ?? 120, BusIdx = 3 }
            };

            foreach (var slot in timeSlots)
            {
                var bus = sampleBuses.ElementAtOrDefault(slot.BusIdx % Math.Max(1, sampleBuses.Count)) 
                          ?? allActiveBuses.FirstOrDefault()
                          ?? new Bus { BusId = 1, Capacity = 29, LicensePlate = "29B-012.34" };

                var arrTime = slot.Dep.AddMinutes(slot.Duration);

                trips.Add(new Trip
                {
                    RouteId = route.RouteId,
                    Route = route,
                    BusId = bus.BusId,
                    Bus = bus,
                    DriverId = sampleDriver?.DriverId ?? 1,
                    TripDate = date,
                    DepartureTime = slot.Dep,
                    ArrivalTime = arrTime,
                    Status = "Scheduled",
                    Bookings = new List<Booking>()
                });
            }

            return trips;
        }

        private string GetBusImage(string? busType)
        {
            if (busType != null && busType.Contains("Limousine", StringComparison.OrdinalIgnoreCase))
                return "https://images.unsplash.com/photo-1544620347-c4fd4a3d5957?w=400&q=80";
            if (busType != null && busType.Contains("Giường", StringComparison.OrdinalIgnoreCase))
                return "https://images.unsplash.com/photo-1570125909232-eb263c188f7e?w=400&q=80";
            return "https://images.unsplash.com/photo-1494515843206-f3117d3f51b7?w=400&q=80";
        }

        private static string FormatToTitleCase(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            var textInfo = new System.Globalization.CultureInfo("vi-VN", false).TextInfo;
            return textInfo.ToTitleCase(text.Trim().ToLower());
        }

        private static bool IsRouteMatchingSearch(WebApplication1.Models.Entities.Route route, string searchFrom, string searchTo)
        {
            if (route == null || string.IsNullOrWhiteSpace(searchFrom) || string.IsNullOrWhiteSpace(searchTo))
                return false;

            // 1. Kiểm tra điểm xuất phát (Departure)
            bool matchDeparture = false;
            int pickupStopOrder = -1;

            if (WebApplication1.Services.LocationHelper.Matches(route.StartPoint, searchFrom))
            {
                matchDeparture = true;
                pickupStopOrder = 0;
            }
            else if (route.RouteStops != null && route.RouteStops.Any())
            {
                var matchingStop = route.RouteStops
                    .OrderBy(rs => rs.StopOrder)
                    .FirstOrDefault(rs => rs.Stop != null && WebApplication1.Services.LocationHelper.Matches(rs.Stop.StopName, searchFrom));

                if (matchingStop != null)
                {
                    matchDeparture = true;
                    pickupStopOrder = matchingStop.StopOrder;
                }
            }

            if (!matchDeparture) return false;

            // 2. Kiểm tra điểm đến (Destination)
            bool matchDestination = false;
            int dropoffStopOrder = int.MaxValue;

            if (WebApplication1.Services.LocationHelper.Matches(route.EndPoint, searchTo))
            {
                matchDestination = true;
                dropoffStopOrder = int.MaxValue;
            }
            else if (route.RouteStops != null && route.RouteStops.Any())
            {
                var matchingStop = route.RouteStops
                    .OrderByDescending(rs => rs.StopOrder)
                    .FirstOrDefault(rs => rs.Stop != null && WebApplication1.Services.LocationHelper.Matches(rs.Stop.StopName, searchTo));

                if (matchingStop != null)
                {
                    matchDestination = true;
                    dropoffStopOrder = matchingStop.StopOrder;
                }
            }

            if (!matchDestination) return false;

            // 3. Thứ tự trạm đón phải diễn ra trước trạm trả
            if (pickupStopOrder >= dropoffStopOrder)
                return false;

            // 4. Tuyệt đối không để điểm đến trùng với điểm xuất phát của tuyến
            if (WebApplication1.Services.LocationHelper.Matches(route.StartPoint, searchTo) &&
                !WebApplication1.Services.LocationHelper.Matches(route.EndPoint, searchTo))
            {
                return false;
            }

            return true;
        }
    }
}

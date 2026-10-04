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

        public TripController(ApplicationDbContext context, WebApplication1.Services.BusLayout.IBusLayoutService busLayoutService)
        {
            _context = context;
            _busLayoutService = busLayoutService;
        }

        [HttpGet]
        public IActionResult Search(
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
                    .Where(r => 
                        fromAliases.Any(fa => WebApplication1.Services.LocationHelper.Matches(r.StartPoint, fa) || 
                                              WebApplication1.Services.LocationHelper.Matches(r.RouteName, fa) ||
                                              r.RouteStops.Any(rs => WebApplication1.Services.LocationHelper.Matches(rs.Stop.StopName, fa))) &&
                        toAliases.Any(ta => WebApplication1.Services.LocationHelper.Matches(r.EndPoint, ta) || 
                                            WebApplication1.Services.LocationHelper.Matches(r.RouteName, ta) ||
                                            r.RouteStops.Any(rs => WebApplication1.Services.LocationHelper.Matches(rs.Stop.StopName, ta)))
                    )
                    .ToList();

                // Lọc các chuyến xe thực tế trong DB
                foreach (var route in queryRoutes)
                {
                    var activeTrips = route.Trips
                        .Where(t => t.TripDate == tripDateOnly && (t.Status == null || !t.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase)))
                        .ToList();

                    // Nếu ngày tìm kiếm chưa có chuyến tạo sẵn trong DB, tự động sinh các chuyến theo Route thực tế
                    if (!activeTrips.Any())
                    {
                        activeTrips = GenerateScheduledTripsForRoute(route, tripDateOnly);
                    }

                    foreach (var trip in activeTrips)
                    {
                        var validTickets = trip.Bookings?
                            .Where(bk => bk.Status == null || !bk.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
                            .SelectMany(bk => bk.Tickets ?? Enumerable.Empty<Ticket>())
                            .Where(tk => tk.Status == null || !tk.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
                            .ToList() ?? new List<Ticket>();

                        var bookedSeatNumbers = validTickets
                            .Where(tk => !string.IsNullOrEmpty(tk.SeatNumber))
                            .Select(tk => tk.SeatNumber!)
                            .ToHashSet(StringComparer.OrdinalIgnoreCase);

                        var totalCapacity = trip.Bus?.Capacity ?? 29;
                        var bookedCount = validTickets.Count;
                        var availableSeats = Math.Max(0, totalCapacity - bookedCount);

                        if (availableSeats <= 0) continue;

                        var boardingStops = route.RouteStops
                            .OrderBy(rs => rs.StopOrder)
                            .Take(3)
                            .Select(rs => $"{rs.Stop.StopName} ({(rs.DepartureTime.HasValue ? rs.DepartureTime.Value.ToString(@"hh\:mm") : trip.DepartureTime.ToString(@"hh\:mm"))})")
                            .ToList();

                        var dropOffStops = route.RouteStops
                            .OrderBy(rs => rs.StopOrder)
                            .Skip(Math.Max(0, route.RouteStops.Count - 2))
                            .Select(rs => $"{rs.Stop.StopName} ({(rs.ArrivalTime.HasValue ? rs.ArrivalTime.Value.ToString(@"hh\:mm") : (trip.ArrivalTime.HasValue ? trip.ArrivalTime.Value.ToString(@"hh\:mm") : "11:00"))})")
                            .ToList();

                        if (!boardingStops.Any()) boardingStops.Add($"{route.StartPoint} ({trip.DepartureTime:hh\\:mm})");
                        if (!dropOffStops.Any()) dropOffStops.Add($"{route.EndPoint} ({(trip.ArrivalTime.HasValue ? trip.ArrivalTime.Value.ToString(@"hh\:mm") : "10:30")})");

                        var depTimeSpan = trip.DepartureTime.ToTimeSpan();
                        var effectiveDistance = route.Distance ?? WebApplication1.Services.RoutePricingService.GetRouteMetrics(route.StartPoint, route.EndPoint).Distance;
                        var arrTimeSpan = trip.ArrivalTime?.ToTimeSpan() ?? depTimeSpan.Add(TimeSpan.FromMinutes(route.EstimatedDuration ?? WebApplication1.Services.RoutePricingService.GetRouteMetrics(route.StartPoint, route.EndPoint).EstimatedMinutes));
                        var duration = arrTimeSpan - depTimeSpan;
                        if (duration < TimeSpan.Zero) duration = duration.Add(TimeSpan.FromHours(24));
                        var durationText = $"{(int)duration.TotalHours}h {duration.Minutes}m";

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
                            DeparturePoint = boardingStops.FirstOrDefault() ?? route.StartPoint,
                            ArrivalTime = arrTimeSpan,
                            ArrivalPoint = dropOffStops.LastOrDefault() ?? route.EndPoint,
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

            // Nếu DB chưa có tuyến hoặc chuyến xe phù hợp, sinh danh sách chuyến xe động ĐÚNG TUYẾN NGƯỜI DÙNG TÌM KIẾM
            if (!tripViewModels.Any())
            {
                var dynamicTrips = GenerateDynamicTrips(model.From, model.To, searchDate);
                tripViewModels.AddRange(dynamicTrips);
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

            int fakeId = route.RouteId * 1000;
            foreach (var slot in timeSlots)
            {
                fakeId++;
                var bus = sampleBuses.ElementAtOrDefault(slot.BusIdx % Math.Max(1, sampleBuses.Count)) 
                          ?? new Bus { BusId = 1, Capacity = 29, LicensePlate = "29B-888.88", BusType = new BusType { TypeName = "Xe 29 chỗ" } };

                var arrTime = slot.Dep.AddMinutes(slot.Duration);

                trips.Add(new Trip
                {
                    TripId = fakeId,
                    RouteId = route.RouteId,
                    Route = route,
                    BusId = bus.BusId,
                    Bus = bus,
                    DriverId = sampleDriver?.DriverId ?? 1,
                    Driver = sampleDriver ?? new Driver { DriverId = 1, LicenseNo = "B123456789", Status = "Active", User = new User { FullName = "Nguyễn Văn An", Phone = "0987654321" } },
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

        /// <summary>
        /// Sinh danh sách chuyến xe động ĐÚNG 100% ĐIỂM ĐI VÀ ĐIỂM ĐẾN người dùng tìm kiếm
        /// với 4 khung giờ và 4 dòng xe đa dạng, gắn biển số thông minh theo tỉnh thành
        /// </summary>
        private List<TripItemViewModel> GenerateDynamicTrips(string from, string to, DateTime tripDate)
        {
            var trips = new List<TripItemViewModel>();
            var cleanFrom = FormatToTitleCase(from);
            var cleanTo = FormatToTitleCase(to);

            var fromCodes = WebApplication1.Services.ProvinceLicenseHelper.GetAllCodes(from);
            var toCodes = WebApplication1.Services.ProvinceLicenseHelper.GetAllCodes(to);

            // Tra cứu khoảng cách (km) và số phút di chuyển thực tế theo cự ly và địa hình Việt Nam
            var metrics = WebApplication1.Services.RoutePricingService.GetRouteMetrics(from, to);

            // Tìm xe phù hợp theo từng loại từ DB nếu có
            string? plate1 = null, plate2 = null, plate3 = null, plate4 = null;
            try
            {
                var allActiveBuses = _context.Buses
                    .Where(b => b.Status == null || b.Status == "Active" || b.Status == "ACTIVE")
                    .ToList();

                var selectedBuses = WebApplication1.Services.ProvinceLicenseHelper.SelectDiverseBusesForRoute(allActiveBuses, from, to, 4);
                if (selectedBuses.Count > 0) plate1 = selectedBuses[0].LicensePlate;
                if (selectedBuses.Count > 1) plate2 = selectedBuses[1].LicensePlate;
                if (selectedBuses.Count > 2) plate3 = selectedBuses[2].LicensePlate;
                if (selectedBuses.Count > 3) plate4 = selectedBuses[3].LicensePlate;
            }
            catch
            {
                // Fallback nếu kết nối cơ sở dữ liệu tạm gián đoạn
            }

            // Nếu DB chưa có đủ biển số tương ứng, tự động sinh biển số thực tế chuẩn tỉnh thành
            var fallbackPlates = WebApplication1.Services.ProvinceLicenseHelper.GenerateDiverseLicensePlates(from, to);
            plate1 ??= fallbackPlates[0];
            plate2 ??= fallbackPlates[1];
            plate3 ??= fallbackPlates[2];
            plate4 ??= fallbackPlates[3];
            var plates = new[] { plate1, plate2, plate3, plate4 };

            // Lấy danh sách 4 cấu hình chuyến xe đa dạng phù hợp nhất với cự ly tuyến
            var tripConfigs = WebApplication1.Services.RoutePricingService.GetTripConfigsForRoute(metrics.Distance);

            int hours = metrics.EstimatedMinutes / 60;
            int mins = metrics.EstimatedMinutes % 60;
            string durationText = hours > 0 ? (mins > 0 ? $"{hours}h {mins}m" : $"{hours}h 00m") : $"{mins}m";

            // Danh sách ghế mẫu đã được đặt để tạo tính chân thực
            var sampleBookedSeatsList = new[]
            {
                new[] { "A02", "A05", "A12" },
                new[] { "A01", "A02", "B01", "B02", "C05", "D05" },
                new[] { "A01", "A02", "B02", "C03", "A08", "B10" },
                new[] { "VIP-T1-01", "VIP-T1-03", "VIP-T2-02" }
            };

            for (int i = 0; i < tripConfigs.Count; i++)
            {
                var cfg = tripConfigs[i];
                var plate = plates[i % plates.Length];
                var provName = WebApplication1.Services.ProvinceLicenseHelper.GetProvinceName(plate);

                // Tính giá vé chuẩn xác theo khoảng cách, loại xe, sức chứa và khung giờ chuyến
                decimal tripPrice = WebApplication1.Services.RoutePricingService.CalculateTripPrice(metrics.Distance, cfg.BusTypeName, cfg.Capacity, cfg.SlotIndex);
                decimal originalPrice = Math.Round(tripPrice * 1.15m / 5000m, MidpointRounding.AwayFromZero) * 5000m;

                var depTime = cfg.DepartureTime;
                var totalMinutes = (int)depTime.TotalMinutes + metrics.EstimatedMinutes;
                var arrTime = TimeSpan.FromMinutes(totalMinutes % (24 * 60));

                var booked = sampleBookedSeatsList[i % sampleBookedSeatsList.Length];
                var floors = _busLayoutService.GenerateFloorLayouts(cfg.BusTypeName, cfg.Capacity, booked, tripPrice);
                var flatSeats = floors.SelectMany(f => f.Seats)
                    .Where(s => s.IsBookable)
                    .Select(s => new SeatItemViewModel
                    {
                        SeatCode = s.SeatCode,
                        Floor = s.Floor,
                        Price = s.Price,
                        Status = s.Status
                    }).ToList();

                var depPoint = i % 2 == 0 ? $"Bến xe Trung Tâm {cleanFrom}" : $"Văn phòng đón trả {cleanFrom}";
                var arrPoint = i % 2 == 0 ? $"Bến xe {cleanTo}" : $"Văn phòng trung tâm {cleanTo}";

                var boardingList = new List<string>
                {
                    $"{depPoint} ({depTime:hh\\:mm})",
                    $"Điểm dừng chân {cleanFrom} ({depTime.Add(TimeSpan.FromMinutes(15)):hh\\:mm})"
                };

                var dropOffList = new List<string>
                {
                    $"Cửa ngõ {cleanTo} ({arrTime.Subtract(TimeSpan.FromMinutes(15)):hh\\:mm})",
                    $"{arrPoint} ({arrTime:hh\\:mm})"
                };

                int availableCount = Math.Max(5, cfg.Capacity - booked.Length);

                trips.Add(new TripItemViewModel
                {
                    TripId = 101 + i,
                    OperatorName = cfg.OperatorName,
                    Rating = cfg.Rating,
                    ReviewCount = cfg.ReviewCount,
                    BusTypeName = cfg.BusTypeName,
                    BusImage = GetBusImage(cfg.BusTypeName),
                    LicensePlate = plate,
                    ProvinceName = provName,
                    IsMatchingDeparture = fromCodes.Any(c => plate.StartsWith(c)),
                    IsMatchingDestination = toCodes.Any(c => plate.StartsWith(c)),
                    DepartureTime = depTime,
                    DeparturePoint = depPoint,
                    ArrivalTime = arrTime,
                    ArrivalPoint = arrPoint,
                    DurationText = durationText,
                    Price = tripPrice,
                    OriginalPrice = originalPrice,
                    AvailableSeats = availableCount,
                    TotalCapacity = cfg.Capacity,
                    IsFlashSale = cfg.IsFlashSale,
                    FlashSaleText = cfg.FlashSaleText ?? "ƯU ĐÃI ĐẶT SỚM",
                    NoticeText = string.IsNullOrEmpty(cfg.NoticeText) 
                        ? $"Chuyến khởi hành ngày {tripDate:dd/MM/yyyy} tuyến {cleanFrom} - {cleanTo}" 
                        : cfg.NoticeText,
                    BoardingPoints = boardingList,
                    DropOffPoints = dropOffList,
                    BusTypeCode = cfg.BusTypeCode,
                    FloorLayouts = floors,
                    Seats = flatSeats,
                    RouteCode = $"{fromCodes.FirstOrDefault() ?? "HN"}-{toCodes.FirstOrDefault() ?? "HP"}-{i + 1:D2}",
                    RouteName = $"{cleanFrom} - {cleanTo}",
                    DriverName = i % 2 == 0 ? $"Nguyễn Văn Tuấn (Tài xế {cleanFrom})" : $"Trần Đình Trọng (Tài xế {cleanTo})",
                    DriverPhone = i % 2 == 0 ? "0988 777 999" : "0912 345 678",
                    EstimatedDuration = metrics.EstimatedMinutes,
                    Distance = metrics.Distance
                });
            }

            return trips;
        }

        private static string FormatToTitleCase(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            var textInfo = new System.Globalization.CultureInfo("vi-VN", false).TextInfo;
            return textInfo.ToTitleCase(text.Trim().ToLower());
        }
    }
}

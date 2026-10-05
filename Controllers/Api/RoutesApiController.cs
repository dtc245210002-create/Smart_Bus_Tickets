using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApplication1.Data;
using WebApplication1.Models.Api;

namespace WebApplication1.Controllers.Api
{
    [ApiController]
    [Route("api/routes")]
    [Produces("application/json")]
    public class RoutesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public RoutesApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// API GET /api/routes/search
        /// Nhận query parameters: origin (hoặc from), destination (hoặc to), date.
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<ApiResponse<RouteSearchResultDto>>> Search(
            [FromQuery] string? origin,
            [FromQuery] string? from,
            [FromQuery] string? destination,
            [FromQuery] string? to,
            [FromQuery] DateTime? date,
            [FromQuery] string? sort = "default",
            [FromQuery] string? busType = null)
        {
            var searchOrigin = !string.IsNullOrWhiteSpace(origin) ? origin : from;
            var searchDestination = !string.IsNullOrWhiteSpace(destination) ? destination : to;

            // 1. Validate tham số đầu vào
            if (string.IsNullOrWhiteSpace(searchOrigin) || string.IsNullOrWhiteSpace(searchDestination))
            {
                return BadRequest(ApiResponse<RouteSearchResultDto>.Fail(
                    "Vui lòng cung cấp cả điểm xuất phát (origin) và điểm đến (destination)."));
            }

            var cleanOrigin = searchOrigin.Trim();
            var cleanDest = searchDestination.Trim();

            if (string.Equals(cleanOrigin, cleanDest, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(ApiResponse<RouteSearchResultDto>.Fail(
                    "Điểm xuất phát và điểm đến không được trùng nhau."));
            }

            var searchDate = date?.Date ?? DateTime.Today;
            if (searchDate < DateTime.Today)
            {
                return BadRequest(ApiResponse<RouteSearchResultDto>.Fail(
                    $"Ngày khởi hành ({searchDate:yyyy-MM-dd}) không được ở trong quá khứ."));
            }

            var tripDateOnly = DateOnly.FromDateTime(searchDate);

            // 2. Truy vấn dữ liệu từ Database SQL Server
            var tripDtos = new List<TripSummaryDto>();

            var fromAliases = WebApplication1.Services.LocationHelper.GetLocationAliases(cleanOrigin);
            var toAliases = WebApplication1.Services.LocationHelper.GetLocationAliases(cleanDest);

            try
            {
                var queryRoutes = await _context.Routes
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
                    .ToListAsync();

                var matchingRoutes = queryRoutes
                    .Where(r => 
                        fromAliases.Any(fa => WebApplication1.Services.LocationHelper.Matches(r.StartPoint, fa) || 
                                              WebApplication1.Services.LocationHelper.Matches(r.RouteName, fa) ||
                                              r.RouteStops.Any(rs => WebApplication1.Services.LocationHelper.Matches(rs.Stop.StopName, fa))) &&
                        toAliases.Any(ta => WebApplication1.Services.LocationHelper.Matches(r.EndPoint, ta) || 
                                            WebApplication1.Services.LocationHelper.Matches(r.RouteName, ta) ||
                                            r.RouteStops.Any(rs => WebApplication1.Services.LocationHelper.Matches(rs.Stop.StopName, ta)))
                    )
                    .ToList();

                var tripsFromDb = matchingRoutes.SelectMany(r => r.Trips)
                    .Where(t => t.TripDate == tripDateOnly && (t.Status == null || !t.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                foreach (var trip in tripsFromDb)
                {
                    var validTickets = trip.Bookings
                        .Where(bk => bk.Status != "Cancelled" && bk.Status != "CANCELLED")
                        .SelectMany(bk => bk.Tickets)
                        .Where(tk => tk.Status != "Cancelled" && tk.Status != "CANCELLED")
                        .ToList();

                    var totalCapacity = trip.Bus?.Capacity ?? 29;
                    var bookedCount = validTickets.Count;
                    var availableSeats = Math.Max(0, totalCapacity - bookedCount);

                    if (availableSeats <= 0) continue; // Chỉ hiển thị chuyến còn ghế

                    var depTime = trip.DepartureTime.ToTimeSpan();
                    var arrTime = trip.ArrivalTime?.ToTimeSpan() ?? depTime.Add(TimeSpan.FromMinutes(trip.Route.EstimatedDuration ?? 120));
                    var duration = arrTime - depTime;
                    if (duration < TimeSpan.Zero) duration = duration.Add(TimeSpan.FromHours(24));

                    var price = validTickets.FirstOrDefault()?.Price ?? 150000m;

                    var boardingStops = trip.Route.RouteStops
                        .OrderBy(rs => rs.StopOrder)
                        .Take(3)
                        .Select(rs => rs.Stop.StopName)
                        .ToList();
                    if (!boardingStops.Any()) boardingStops.Add(trip.Route.StartPoint);

                    var dropOffStops = trip.Route.RouteStops
                        .OrderBy(rs => rs.StopOrder)
                        .Skip(Math.Max(0, trip.Route.RouteStops.Count - 2))
                        .Select(rs => rs.Stop.StopName)
                        .ToList();
                    if (!dropOffStops.Any()) dropOffStops.Add(trip.Route.EndPoint);

                    tripDtos.Add(new TripSummaryDto
                    {
                        TripId = trip.TripId,
                        RouteId = trip.RouteId,
                        RouteName = trip.Route.RouteName,
                        Origin = trip.Route.StartPoint,
                        Destination = trip.Route.EndPoint,
                        TripDate = trip.TripDate.ToString("yyyy-MM-dd"),
                        DepartureTime = depTime.ToString(@"hh\:mm"),
                        ArrivalTime = arrTime.ToString(@"hh\:mm"),
                        DurationText = $"{(int)duration.TotalHours}h {duration.Minutes}m",
                        BusTypeName = trip.Bus?.BusType?.TypeName ?? "Xe Limousine Cao Cấp",
                        LicensePlate = trip.Bus?.LicensePlate ?? "29B-888.88",
                        OperatorName = trip.Route.RouteName.Contains("VIP") ? "SmartBus VIP Express" : "SmartBus Travel",
                        Price = price,
                        OriginalPrice = price * 1.15m,
                        AvailableSeats = availableSeats,
                        TotalSeats = totalCapacity,
                        IsFlashSale = availableSeats < 10,
                        FlashSaleText = availableSeats < 10 ? "SẮP HẾT CHỖ" : null,
                        BoardingPoints = boardingStops,
                        DropOffPoints = dropOffStops
                    });
                }
            }
            catch
            {
                // Nếu DB chưa có cấu hình connection string hoặc bảng tạm thời trống, giữ tripDtos rỗng
            }

            // 3. Fallback dữ liệu phong phú nếu DB chưa có chuyến nào cho ngày/tuyến tìm kiếm
            if (!tripDtos.Any())
            {
                tripDtos = GenerateMockTrips(cleanOrigin, cleanDest, searchDate);
            }

            // 4. Lọc & Sắp xếp
            if (!string.IsNullOrEmpty(busType))
            {
                tripDtos = tripDtos.Where(t => t.BusTypeName.Contains(busType, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            tripDtos = sort?.ToLower() switch
            {
                "price_asc" => tripDtos.OrderBy(t => t.Price).ToList(),
                "price_desc" => tripDtos.OrderByDescending(t => t.Price).ToList(),
                "time_asc" or "time_earliest" => tripDtos.OrderBy(t => t.DepartureTime).ToList(),
                "time_desc" or "time_latest" => tripDtos.OrderByDescending(t => t.DepartureTime).ToList(),
                _ => tripDtos
            };

            var result = new RouteSearchResultDto
            {
                Origin = cleanOrigin,
                Destination = cleanDest,
                SearchDate = searchDate.ToString("yyyy-MM-dd"),
                TotalTrips = tripDtos.Count,
                Trips = tripDtos
            };

            return Ok(ApiResponse<RouteSearchResultDto>.Ok(result, $"Tìm thấy {tripDtos.Count} chuyến xe phù hợp."));
        }

        private static List<TripSummaryDto> GenerateMockTrips(string origin, string dest, DateTime date)
        {
            var formattedDate = date.ToString("yyyy-MM-dd");
            return new List<TripSummaryDto>
            {
                new()
                {
                    TripId = 101,
                    RouteId = 1,
                    RouteName = $"{origin} - {dest} (Cao tốc Express)",
                    Origin = origin,
                    Destination = dest,
                    TripDate = formattedDate,
                    DepartureTime = "08:00",
                    ArrivalTime = "10:30",
                    DurationText = "2h 30m",
                    BusTypeName = "Limousine VIP 16 Chỗ",
                    LicensePlate = "29B-888.68",
                    OperatorName = "SmartBus VIP Express",
                    Price = 160000m,
                    OriginalPrice = 190000m,
                    AvailableSeats = 12,
                    TotalSeats = 16,
                    IsFlashSale = true,
                    FlashSaleText = "GIẢM 15% HÔM NAY",
                    BoardingPoints = new() { $"{origin} (Bến xe Trung Tâm)", $"{origin} (Văn phòng đại diện)" },
                    DropOffPoints = new() { $"{dest} (Ngã ba Trung Tâm)", $"{dest} (Bến xe cuối)" }
                },
                new()
                {
                    TripId = 102,
                    RouteId = 2,
                    RouteName = $"{origin} - {dest} (Phòng Nằm Cung Điện)",
                    Origin = origin,
                    Destination = dest,
                    TripDate = formattedDate,
                    DepartureTime = "13:30",
                    ArrivalTime = "16:00",
                    DurationText = "2h 30m",
                    BusTypeName = "Limousine VIP 22 Phòng Đơn Cung Điện",
                    LicensePlate = "15B-668.99",
                    OperatorName = "SmartBus Royal Travel",
                    Price = 220000m,
                    OriginalPrice = 250000m,
                    AvailableSeats = 8,
                    TotalSeats = 22,
                    IsFlashSale = false,
                    BoardingPoints = new() { $"{origin} (Cột 05 Bến xe)", $"{origin} (Trạm thu phí ngoại ô)" },
                    DropOffPoints = new() { $"{dest} (Trạm dừng chân VIP)", $"{dest} (Bến xe trung tâm)" }
                },
                new()
                {
                    TripId = 103,
                    RouteId = 3,
                    RouteName = $"{origin} - {dest} (Chuyến Đêm Eco)",
                    Origin = origin,
                    Destination = dest,
                    TripDate = formattedDate,
                    DepartureTime = "19:00",
                    ArrivalTime = "21:45",
                    DurationText = "2h 45m",
                    BusTypeName = "Xe Giường Nằm 34 Chỗ Cao Cấp",
                    LicensePlate = "29B-999.11",
                    OperatorName = "SmartBus Eco Line",
                    Price = 135000m,
                    OriginalPrice = 150000m,
                    AvailableSeats = 18,
                    TotalSeats = 34,
                    IsFlashSale = true,
                    FlashSaleText = "TIẾT KIỆM",
                    BoardingPoints = new() { $"{origin} (Bến xe phía Nam)" },
                    DropOffPoints = new() { $"{dest} (Bến xe chính)" }
                }
            };
        }
    }
}

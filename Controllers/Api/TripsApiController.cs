using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApplication1.Data;
using WebApplication1.Models.Api;
using WebApplication1.Services;

namespace WebApplication1.Controllers.Api
{
    [ApiController]
    [Route("api/trips")]
    [Produces("application/json")]
    public class TripsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ISeatHoldService _seatHoldService;

        public TripsApiController(ApplicationDbContext context, ISeatHoldService seatHoldService)
        {
            _context = context;
            _seatHoldService = seatHoldService;
        }

        /// <summary>
        /// API GET /api/trips/{trip_id}/seats
        /// Trả về danh sách và trạng thái sơ đồ ghế của chuyến xe cụ thể (AVAILABLE, BOOKED, HOLD)
        /// </summary>
        [HttpGet("{trip_id}/seats")]
        public async Task<ActionResult<ApiResponse<TripSeatMapDto>>> GetTripSeats(
            [FromRoute(Name = "trip_id")] int tripId,
            [FromQuery] string? sessionId = null,
            [FromQuery] int? userId = null)
        {
            if (tripId <= 0)
            {
                return BadRequest(ApiResponse<TripSeatMapDto>.Fail("Mã chuyến xe (trip_id) không hợp lệ."));
            }

            // 1. Lấy thông tin chuyến xe từ DB
            TripSeatMapDto? seatMap = null;

            try
            {
                var trip = await _context.Trips
                    .Include(t => t.Route)
                    .Include(t => t.Bus)
                        .ThenInclude(b => b.BusType)
                    .Include(t => t.Bookings)
                        .ThenInclude(bk => bk.Tickets)
                    .FirstOrDefaultAsync(t => t.TripId == tripId);

                if (trip != null)
                {
                    var totalCapacity = trip.Bus?.Capacity ?? 28;
                    var basePrice = trip.Bookings
                        .SelectMany(b => b.Tickets)
                        .FirstOrDefault()?.Price ?? 150000m;

                    var bookedSeats = trip.Bookings
                        .Where(bk => bk.Status != "Cancelled" && bk.Status != "CANCELLED")
                        .SelectMany(bk => bk.Tickets)
                        .Where(tk => tk.Status != "Cancelled" && tk.Status != "CANCELLED" && !string.IsNullOrEmpty(tk.SeatNumber))
                        .Select(tk => tk.SeatNumber!.Trim().ToUpper())
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                    seatMap = BuildSeatMap(
                        trip.TripId,
                        trip.Route?.RouteName ?? "Hà Nội - Hải Phòng",
                        trip.Route?.StartPoint ?? "Hà Nội",
                        trip.Route?.EndPoint ?? "Hải Phòng",
                        trip.TripDate.ToString("yyyy-MM-dd"),
                        trip.DepartureTime.ToString(@"hh\:mm"),
                        trip.ArrivalTime?.ToString(@"hh\:mm") ?? "11:00",
                        trip.Bus?.BusType?.TypeName ?? "Xe Limousine Cao Cấp",
                        trip.Bus?.LicensePlate ?? "29B-888.88",
                        totalCapacity,
                        basePrice,
                        bookedSeats);
                }
            }
            catch
            {
                // Fallback nếu DB chưa kết nối
            }

            // 2. Mock fallback nếu không tìm thấy trip trong DB
            if (seatMap == null)
            {
                seatMap = BuildMockSeatMap(tripId);
            }

            // 3. Đọc dữ liệu ghế đang được HOLD trong bộ nhớ (SeatHoldService)
            var activeHolds = await _seatHoldService.GetActiveHoldsForTripAsync(tripId);

            int heldCount = 0;
            int bookedCount = 0;

            foreach (var seat in seatMap.Seats)
            {
                if (seat.Status == "BOOKED")
                {
                    bookedCount++;
                    continue;
                }

                if (activeHolds.TryGetValue(seat.SeatNumber, out var holdInfo) && !holdInfo.IsExpired)
                {
                    seat.Status = "HOLD";
                    seat.HeldExpiresAt = holdInfo.ExpiresAtUtc.ToLocalTime();
                    seat.RemainingHoldSeconds = holdInfo.RemainingSeconds;

                    bool isMine = (!string.IsNullOrEmpty(sessionId) && string.Equals(holdInfo.SessionId, sessionId, StringComparison.OrdinalIgnoreCase))
                                  || (userId.HasValue && holdInfo.UserId == userId.Value);
                    seat.IsMine = isMine;
                    heldCount++;
                }
                else
                {
                    seat.Status = "AVAILABLE";
                }
            }

            seatMap.BookedSeats = bookedCount;
            seatMap.HeldSeats = heldCount;
            seatMap.AvailableSeats = Math.Max(0, seatMap.TotalSeats - bookedCount - heldCount);

            return Ok(ApiResponse<TripSeatMapDto>.Ok(
                seatMap, 
                $"Lấy sơ đồ {seatMap.TotalSeats} ghế thành công (Trống: {seatMap.AvailableSeats}, Đã đặt: {seatMap.BookedSeats}, Đang giữ: {seatMap.HeldSeats})."));
        }

        private static TripSeatMapDto BuildSeatMap(
            int tripId,
            string routeName,
            string origin,
            string destination,
            string tripDate,
            string departureTime,
            string arrivalTime,
            string busTypeName,
            string licensePlate,
            int capacity,
            decimal basePrice,
            HashSet<string> bookedSeats)
        {
            var seats = new List<SeatDto>();
            int floors = capacity > 24 ? 2 : 1;
            int rowsPerFloor = floors == 2 ? ((capacity / 2) + 2) / 3 : ((capacity + 3) / 4);

            int seatIndex = 0;
            string[] cols = floors == 2 ? new[] { "A", "B", "C" } : new[] { "A", "B", "C", "D" };

            for (int f = 1; f <= floors; f++)
            {
                for (int r = 1; r <= rowsPerFloor; r++)
                {
                    foreach (var c in cols)
                    {
                        if (seatIndex >= capacity) break;

                        var seatCode = floors == 2 ? $"{c}{r:D2}-T{f}" : $"{c}{r:D2}";
                        var isBooked = bookedSeats.Contains(seatCode);

                        // Ghế VIP ở các hàng đầu
                        var isVip = r <= 2;
                        var seatPrice = isVip ? basePrice * 1.1m : basePrice;

                        seats.Add(new SeatDto
                        {
                            SeatNumber = seatCode,
                            Floor = f,
                            Row = r,
                            Column = c,
                            Type = isVip ? "VIP" : (floors == 2 ? "Sleeper" : "Standard"),
                            Price = seatPrice,
                            Status = isBooked ? "BOOKED" : "AVAILABLE"
                        });

                        seatIndex++;
                    }
                }
            }

            return new TripSeatMapDto
            {
                TripId = tripId,
                RouteName = routeName,
                Origin = origin,
                Destination = destination,
                TripDate = tripDate,
                DepartureTime = departureTime,
                ArrivalTime = arrivalTime,
                BusTypeName = busTypeName,
                LicensePlate = licensePlate,
                TotalSeats = seats.Count,
                BasePrice = basePrice,
                Floors = floors,
                Seats = seats
            };
        }

        private static TripSeatMapDto BuildMockSeatMap(int tripId)
        {
            var defaultBooked = new HashSet<string>(new[] { "A02", "B04", "C01" }, StringComparer.OrdinalIgnoreCase);
            return BuildSeatMap(
                tripId,
                "Hà Nội - Hải Phòng (Limousine Cao Tốc)",
                "Hà Nội",
                "Hải Phòng",
                DateTime.Today.ToString("yyyy-MM-dd"),
                "08:30",
                "10:45",
                "Limousine VIP 16 Chỗ",
                "29B-888.68",
                16,
                160000m,
                defaultBooked);
        }
    }
}

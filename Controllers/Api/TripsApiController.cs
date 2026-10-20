using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models.Api;
using WebApplication1.Services;
using WebApplication1.Services.BusLayout;

namespace WebApplication1.Controllers.Api;

[ApiController]
[Route("api/trips")]
[Produces("application/json")]
public class TripsApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    public TripsApiController(ApplicationDbContext context, ISeatHoldService seatHoldService) => _context = context;

    [HttpGet("{trip_id}/seats")]
    public async Task<ActionResult<ApiResponse<TripSeatMapDto>>> GetTripSeats(
        [FromRoute(Name = "trip_id")] int tripId, [FromQuery] string? sessionId = null, [FromQuery] int? userId = null)
    {
        if (tripId <= 0) return BadRequest(ApiResponse<TripSeatMapDto>.Fail("TripId không hợp lệ."));
        try
        {
            var trip = await _context.Trips.AsNoTracking().Include(t => t.Route)
                .Include(t => t.Bus).ThenInclude(b => b.BusType)
                .Include(t => t.Bookings).ThenInclude(b => b.Tickets).SingleOrDefaultAsync(t => t.TripId == tripId);
            if (trip == null) return NotFound(ApiResponse<TripSeatMapDto>.Fail("Không tìm thấy chuyến xe."));
            var actorId = User.Identity?.IsAuthenticated == true && int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
            var now = DateTime.Now;
            var active = trip.Bookings.SelectMany(b => b.Tickets).Where(t => BookingsApiController.BlocksSeat(t, now)).ToList();
            var layout = new BusLayoutService().GenerateFloorLayouts(trip.Bus.BusType.TypeName,
                trip.Bus.Capacity, Array.Empty<string>(), BookingsApiController.BaseFare(trip));
            var map = new TripSeatMapDto
            {
                TripId = trip.TripId, RouteName = trip.Route.RouteName, Origin = trip.Route.StartPoint,
                Destination = trip.Route.EndPoint, TripDate = trip.TripDate.ToString("yyyy-MM-dd"),
                DepartureTime = trip.DepartureTime.ToString("HH:mm"), ArrivalTime = trip.ArrivalTime?.ToString("HH:mm") ?? "",
                BusTypeName = trip.Bus.BusType.TypeName, LicensePlate = trip.Bus.LicensePlate,
                BasePrice = BookingsApiController.BaseFare(trip), Floors = layout.Count
            };
            foreach (var seat in layout.SelectMany(f => f.Seats).Where(s => s.IsBookable))
            {
                var allocations = active.Where(t => (t.SeatNumber ?? "").Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Contains(seat.SeatCode, StringComparer.OrdinalIgnoreCase)).ToList();
                var sold = allocations.Any(t => !BookingsApiController.IsHeld(t));
                var hold = allocations.FirstOrDefault(t => BookingsApiController.IsHeld(t));
                var expires = hold?.Booking.BookingTime.AddMinutes(10);
                map.Seats.Add(new SeatDto
                {
                    SeatNumber = seat.SeatCode, Floor = seat.Floor, Row = seat.Row, Column = seat.Col.ToString(),
                    Type = seat.Type.ToString(), Price = seat.Price, Status = sold ? "BOOKED" : hold != null ? "HOLD" : "AVAILABLE",
                    IsMine = !sold && hold != null && actorId > 0 && hold.Booking.UserId == actorId,
                    HeldExpiresAt = expires, RemainingHoldSeconds = expires.HasValue ? Math.Max(0, (int)(expires.Value - now).TotalSeconds) : null
                });
            }
            map.TotalSeats = map.Seats.Count;
            map.BookedSeats = map.Seats.Count(s => s.Status == "BOOKED");
            map.HeldSeats = map.Seats.Count(s => s.Status == "HOLD");
            map.AvailableSeats = map.TotalSeats - map.BookedSeats - map.HeldSeats;
            return Ok(ApiResponse<TripSeatMapDto>.Ok(map));
        }
        catch (Microsoft.Data.SqlClient.SqlException)
        { return StatusCode(503, ApiResponse<TripSeatMapDto>.Fail("Chưa thể tải sơ đồ ghế. Vui lòng thử lại.")); }
    }
}
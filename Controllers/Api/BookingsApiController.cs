using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models.Api;
using WebApplication1.Models.Entities;
using WebApplication1.Services;
using WebApplication1.Services.BusLayout;

namespace WebApplication1.Controllers.Api;

[ApiController]
[Route("api/bookings")]
[Produces("application/json")]
public class BookingsApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    public BookingsApiController(ApplicationDbContext context, ISeatHoldService seatHoldService) => _context = context;
    private int ActorId => User.Identity?.IsAuthenticated == true &&
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    // BookingTime keeps the existing server-local convention until a reviewed migration.
    internal static bool IsHeld(Ticket ticket) => ticket.Status?.ToUpperInvariant() is "HELD" or "PENDING" or "RESERVED";
    internal static bool BlocksSeat(Ticket ticket, DateTime now)
    {
        if (ticket.Status?.ToUpperInvariant() is "CANCELLED" or "EXPIRED") return false;
        if (IsHeld(ticket)) return ticket.Booking.Status?.ToUpperInvariant() != "CANCELLED"
            && ticket.Booking.BookingTime.AddMinutes(10) > now;
        return true;
    }
    internal static decimal BaseFare(Trip trip) => RoutePricingService.CalculateTripPrice(
        trip.Route.Distance ?? 0, trip.Bus.BusType.TypeName, trip.Bus.Capacity);
    private async Task LockTripAsync(int tripId)
    {
        // Transaction-owned SQL lock coordinates all workers, without schema changes.
        var resource = $"SMARTBUS:TripInventory:{tripId}";
        await _context.Database.ExecuteSqlInterpolatedAsync($"DECLARE @r int; EXEC @r = sys.sp_getapplock @Resource={resource}, @LockMode=N'Exclusive', @LockOwner=N'Transaction', @LockTimeout=10000; IF @r < 0 THROW 51000, 'Inventory lock unavailable', 1;");
    }

    [HttpPost("hold-seats")]
    public async Task<ActionResult<ApiResponse<HoldSeatsResponse>>> HoldSeats([FromBody] HoldSeatsRequest request)
    {
        if (ActorId <= 0) return Unauthorized(ApiResponse<HoldSeatsResponse>.Fail("Vui lòng đăng nhập."));
        var seats = request?.SeatNumbers?.Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim().ToUpperInvariant()).Distinct().ToList() ?? new();
        if (request == null || request.TripId <= 0 || seats.Count is < 1 or > 6)
            return BadRequest(ApiResponse<HoldSeatsResponse>.Fail("Chọn từ 1 đến 6 ghế trên chuyến hợp lệ."));
        try
        {
            await using var tx = await _context.Database.BeginTransactionAsync();
            await LockTripAsync(request.TripId);
            var now = DateTime.Now;
            var trip = await _context.Trips.Include(t => t.Route).Include(t => t.Bus).ThenInclude(b => b.BusType)
                .Include(t => t.Bookings).ThenInclude(b => b.Tickets)
                .SingleOrDefaultAsync(t => t.TripId == request.TripId);
            if (trip == null) return NotFound(ApiResponse<HoldSeatsResponse>.Fail("Không tìm thấy chuyến xe."));
            if (trip.Status?.ToUpperInvariant() is "CANCELLED" or "CLOSED" or "DEPARTED" or "COMPLETED"
                || trip.TripDate.ToDateTime(trip.DepartureTime) <= now)
                return Conflict(ApiResponse<HoldSeatsResponse>.Fail("Chuyến xe không còn nhận đặt chỗ."));
            var layout = new BusLayoutService().GenerateFloorLayouts(trip.Bus.BusType.TypeName,
                trip.Bus.Capacity, Array.Empty<string>(), BaseFare(trip));
            var prices = layout.SelectMany(f => f.Seats).Where(s => s.IsBookable)
                .ToDictionary(s => s.SeatCode, s => s.Price, StringComparer.OrdinalIgnoreCase);
            if (seats.Any(s => !prices.ContainsKey(s)))
                return BadRequest(ApiResponse<HoldSeatsResponse>.Fail("Ghế không tồn tại trong sơ đồ chuyến xe."));
            var allocations = trip.Bookings.SelectMany(b => b.Tickets).Where(t => BlocksSeat(t, now)).ToList();
            foreach (var seat in seats)
                if (allocations.Any(t => (t.SeatNumber ?? "").Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Contains(seat, StringComparer.OrdinalIgnoreCase) && (!IsHeld(t) || t.Booking.UserId != ActorId)))
                    return Conflict(ApiResponse<HoldSeatsResponse>.Fail($"Ghế {seat} đã được bán hoặc giữ bởi khách khác."));
            var booking = trip.Bookings.FirstOrDefault(b => b.UserId == ActorId && b.Status == "Pending"
                && b.BookingTime.AddMinutes(10) > now);
            if (booking == null)
            {
                booking = new Booking { TripId = trip.TripId, UserId = ActorId, BookingCode = "BK-" + Guid.NewGuid().ToString("N")[..24],
                    BookingTime = now, Status = "Pending" };
                _context.Bookings.Add(booking);
            }
            var alreadyHeld = booking.Tickets.Where(t => IsHeld(t) && BlocksSeat(t, now)).ToList();
            if (alreadyHeld.Select(t => t.SeatNumber).Union(seats).Count() > 6)
                return BadRequest(ApiResponse<HoldSeatsResponse>.Fail("Mỗi đơn được chọn tối đa 6 ghế."));
            foreach (var seat in seats)
                if (!alreadyHeld.Any(t => string.Equals(t.SeatNumber, seat, StringComparison.OrdinalIgnoreCase)))
                    booking.Tickets.Add(new Ticket { TicketCode = "SBG-" + Guid.NewGuid().ToString("N")[..24], SeatNumber = seat,
                        Price = prices[seat], Status = "HELD" });
            booking.TotalAmount = booking.Tickets.Where(IsHeld).Sum(t => t.Price);
            await _context.SaveChangesAsync();
            await tx.CommitAsync();
            var expires = booking.BookingTime.AddMinutes(10);
            return Ok(ApiResponse<HoldSeatsResponse>.Ok(new HoldSeatsResponse
            {
                HoldId = booking.BookingCode, SessionId = $"actor-{ActorId}", UserId = ActorId,
                TripId = trip.TripId, HeldSeats = booking.Tickets.Where(IsHeld).Select(t => t.SeatNumber!).ToList(),
                TotalPrice = booking.TotalAmount, HeldAt = booking.BookingTime, ExpiresAt = expires,
                RemainingSeconds = Math.Max(0, (int)(expires - now).TotalSeconds)
            }));
        }
        catch (Microsoft.Data.SqlClient.SqlException)
        { return StatusCode(503, ApiResponse<HoldSeatsResponse>.Fail("Chưa thể giữ ghế. Vui lòng thử lại.")); }
    }

    [HttpPost("release-seats")]
    public async Task<ActionResult<ApiResponse<bool>>> ReleaseSeats([FromBody] ReleaseSeatsRequest request)
    {
        if (ActorId <= 0) return Unauthorized(ApiResponse<bool>.Fail("Vui lòng đăng nhập."));
        if (request == null || request.TripId <= 0 || request.SeatNumbers == null)
            return BadRequest(ApiResponse<bool>.Fail("Yêu cầu không hợp lệ."));
        var seats = request.SeatNumbers.Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim().ToUpperInvariant()).Distinct().ToList();
        try
        {
            await using var tx = await _context.Database.BeginTransactionAsync();
            await LockTripAsync(request.TripId);
            var tickets = await _context.Tickets.Include(t => t.Booking).ThenInclude(b => b.Tickets)
                .Where(t => t.Booking.TripId == request.TripId && t.Booking.UserId == ActorId
                    && t.SeatNumber != null && seats.Contains(t.SeatNumber)).ToListAsync();
            foreach (var ticket in tickets.Where(IsHeld)) ticket.Status = "CANCELLED";
            foreach (var booking in tickets.Select(t => t.Booking).Distinct())
                if (booking.Status == "Pending") booking.TotalAmount = booking.Tickets.Where(IsHeld).Sum(t => t.Price);
            await _context.SaveChangesAsync();
            await tx.CommitAsync();
            return Ok(ApiResponse<bool>.Ok(true));
        }
        catch (Microsoft.Data.SqlClient.SqlException)
        { return StatusCode(503, ApiResponse<bool>.Fail("Chưa thể nhả ghế. Vui lòng thử lại.")); }
    }

    [HttpGet("trip-seat-status")]
    public async Task<IActionResult> GetTripSeatStatus([FromQuery] int tripId,
        [FromQuery] string? sessionId = null, [FromQuery] int? userId = null)
    {
        if (ActorId <= 0) return Unauthorized();
        if (tripId <= 0) return BadRequest(new { success = false, message = "TripId không hợp lệ." });
        if (!await _context.Trips.AnyAsync(t => t.TripId == tripId)) return NotFound();
        var tickets = await _context.Tickets.AsNoTracking().Include(t => t.Booking)
            .Where(t => t.Booking.TripId == tripId).ToListAsync();
        var active = tickets.Where(t => BlocksSeat(t, DateTime.Now)).ToList();
        var mine = active.Where(t => IsHeld(t) && t.Booking.UserId == ActorId).ToList();
        return Ok(new { success = true, tripId,
            bookedSeats = active.Except(mine).SelectMany(t => (t.SeatNumber ?? "").Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)).Distinct(),
            myHeldSeats = mine.Select(t => t.SeatNumber).Distinct() });
    }

    [HttpPost("verify-payment")]
    public Task<ActionResult<ApiResponse<VerifyPaymentResponse>>> VerifyPayment([FromBody] VerifyPaymentRequest request)
    {
        ActionResult<ApiResponse<VerifyPaymentResponse>> result = ActorId <= 0
            ? Unauthorized(ApiResponse<VerifyPaymentResponse>.Fail("Vui lòng đăng nhập."))
            : StatusCode(503, ApiResponse<VerifyPaymentResponse>.Fail("Chưa có dịch vụ xác minh thanh toán. Vé chưa được kích hoạt."));
        return Task.FromResult(result);
    }
}
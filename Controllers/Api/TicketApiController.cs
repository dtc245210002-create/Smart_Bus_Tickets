using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;

namespace WebApplication1.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class TicketApiController : ControllerBase
    {
        private readonly SmartBusDbContext _context;

        public TicketApiController(SmartBusDbContext context)
        {
            _context = context;
        }

        // GET: /api/ticketapi/list
        // Lấy danh sách toàn bộ vé đang có trong Database
        [HttpGet("list")]
        public async Task<IActionResult> GetAllTickets()
        {
            var tickets = await _context.Tickets
                .Include(t => t.Booking).ThenInclude(b => b.User)
                .Include(t => t.Booking).ThenInclude(b => b.Trip).ThenInclude(tr => tr.Route)
                .Include(t => t.Booking).ThenInclude(b => b.Trip).ThenInclude(tr => tr.Bus)
                .Select(t => new
                {
                    ticketId = t.TicketId,
                    ticketCode = t.TicketCode,
                    seatNumber = t.SeatNumber,
                    price = t.Price,
                    status = t.Status,
                    bookingCode = t.Booking.BookingCode,
                    passengerName = t.Booking.User.FullName,
                    passengerPhone = t.Booking.User.Phone,
                    routeName = t.Booking.Trip.Route.RouteName,
                    licensePlate = t.Booking.Trip.Bus.LicensePlate,
                    departureTime = t.Booking.Trip.DepartureTime.ToString(@"hh\:mm"),
                    tripDate = t.Booking.Trip.TripDate.ToString("dd/MM/yyyy")
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                total = tickets.Count,
                data = tickets
            });
        }

        // GET: /api/ticketapi/{code}
        // Tra cứu chi tiết 1 vé bằng mã vé (ví dụ: TK001) hoặc TicketId
        [HttpGet("{code}")]
        public async Task<IActionResult> GetTicketByCode(string code)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Booking).ThenInclude(b => b.User)
                .Include(t => t.Booking).ThenInclude(b => b.Trip).ThenInclude(tr => tr.Route)
                .Include(t => t.Booking).ThenInclude(b => b.Trip).ThenInclude(tr => tr.Bus).ThenInclude(b => b.BusType)
                .Include(t => t.Booking).ThenInclude(b => b.Trip).ThenInclude(tr => tr.Driver).ThenInclude(d => d.User)
                .Include(t => t.BoardingStop)
                .Include(t => t.DropOffStop)
                .FirstOrDefaultAsync(t => t.TicketCode == code || t.TicketId.ToString() == code);

            if (ticket == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = $"Không tìm thấy vé với mã '{code}' trong hệ thống."
                });
            }

            var trip = ticket.Booking?.Trip;
            var result = new
            {
                success = true,
                ticketId = ticket.TicketId,
                ticketCode = ticket.TicketCode,
                seatNumber = ticket.SeatNumber,
                price = ticket.Price,
                status = ticket.Status,
                bookingCode = ticket.Booking?.BookingCode,
                bookingTime = ticket.Booking?.BookingTime.ToString("yyyy-MM-dd HH:mm:ss"),
                passenger = new
                {
                    fullName = ticket.Booking?.User?.FullName,
                    phone = ticket.Booking?.User?.Phone,
                    email = ticket.Booking?.User?.Email
                },
                route = new
                {
                    routeCode = trip?.Route?.RouteCode,
                    routeName = trip?.Route?.RouteName,
                    startPoint = trip?.Route?.StartPoint,
                    endPoint = trip?.Route?.EndPoint,
                    distanceKm = trip?.Route?.Distance
                },
                bus = new
                {
                    licensePlate = trip?.Bus?.LicensePlate,
                    busTypeName = trip?.Bus?.BusType?.TypeName,
                    capacity = trip?.Bus?.Capacity
                },
                driver = new
                {
                    driverName = trip?.Driver?.User?.FullName,
                    driverPhone = trip?.Driver?.User?.Phone
                },
                departureTime = trip?.DepartureTime.ToString(@"hh\:mm"),
                tripDate = trip?.TripDate.ToString("dd/MM/yyyy")
            };

            return Ok(result);
        }

        // POST: /api/ticketapi/check-in/{code}
        // Bác tài hoặc nhân viên soát vé quét QR và đánh dấu vé đã sử dụng (USED)
        [HttpPost("check-in/{code}")]
        public async Task<IActionResult> CheckInTicket(string code)
        {
            var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.TicketCode == code || t.TicketId.ToString() == code);
            if (ticket == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy vé để soát." });
            }

            if (ticket.Status == "USED")
            {
                return BadRequest(new { success = false, message = "Vé này ĐÃ ĐƯỢC SOÁT VÉ trước đó!" });
            }

            ticket.Status = "USED";
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = $"Soát vé thành công cho mã vé {ticket.TicketCode}!",
                status = ticket.Status
            });
        }
    }
}

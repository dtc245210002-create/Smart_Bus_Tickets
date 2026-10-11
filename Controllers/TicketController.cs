using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models.ViewModels;

namespace WebApplication1.Controllers
{
    public class TicketController : Controller
    {
        private readonly SmartBusDbContext _context;

        public TicketController(SmartBusDbContext context)
        {
            _context = context;
        }

        // GET: /Ticket/Detail/{id}
        // id có thể là mã vé (ví dụ "TK001") hoặc ID vé (ví dụ 1)
        [HttpGet]
        public async Task<IActionResult> Detail(string? id, string? status = null)
        {
            // 1. Tìm vé trong cơ sở dữ liệu thật kèm các liên kết
            var query = _context.Tickets
                .Include(t => t.Booking).ThenInclude(b => b.User)
                .Include(t => t.Booking).ThenInclude(b => b.Trip).ThenInclude(tr => tr.Route)
                .Include(t => t.Booking).ThenInclude(b => b.Trip).ThenInclude(tr => tr.Bus).ThenInclude(b => b.BusType)
                .Include(t => t.Booking).ThenInclude(b => b.Trip).ThenInclude(tr => tr.Driver).ThenInclude(d => d.User)
                .Include(t => t.BoardingStop)
                .Include(t => t.DropOffStop)
                .AsQueryable();

            Models.Entities.Ticket? ticket = null;

            if (!string.IsNullOrEmpty(id))
            {
                ticket = await query.FirstOrDefaultAsync(t => t.TicketCode == id || t.TicketId.ToString() == id);
            }

            // Nếu không truyền ID hoặc không tìm thấy, lấy vé đầu tiên có sẵn trong DB để demo
            if (ticket == null)
            {
                ticket = await query.FirstOrDefaultAsync();
            }

            if (ticket == null)
            {
                return NotFound("Không tìm thấy dữ liệu vé nào trong hệ thống CSDL.");
            }

            // 2. Trích xuất thông tin liên quan từ CSDL
            var trip = ticket.Booking?.Trip;
            var route = trip?.Route;
            var bus = trip?.Bus;
            var driver = trip?.Driver;
            var passenger = ticket.Booking?.User;

            var ticketStatus = !string.IsNullOrEmpty(status) 
                ? status.ToUpper() 
                : (ticket.Status?.ToUpper() ?? "ACTIVE");

            // 3. Đóng gói dữ liệu từ Database vào ViewModel cho Frontend Razor View
            var model = new TicketDetailViewModel
            {
                TicketId = ticket.TicketId,
                TicketCode = ticket.TicketCode,
                SeatNumber = ticket.SeatNumber ?? "A01",
                Price = ticket.Price,
                Status = ticketStatus,

                BookingId = ticket.BookingId,
                BookingCode = ticket.Booking?.BookingCode ?? "BK001",
                BookingTime = ticket.Booking?.BookingTime ?? DateTime.Now,
                TotalAmount = ticket.Booking?.TotalAmount ?? ticket.Price,

                PassengerName = passenger?.FullName ?? "Hành khách SmartBus",
                PassengerPhone = passenger?.Phone ?? "0912 345 678",
                PassengerEmail = passenger?.Email ?? "passenger@smartbus.vn",

                RouteCode = route?.RouteCode ?? "HN-TN-01",
                RouteName = route?.RouteName ?? "Hà Nội - Thái Nguyên",
                StartPoint = route?.StartPoint ?? "Bến xe Mỹ Đình, Hà Nội",
                EndPoint = route?.EndPoint ?? "Bến xe Thái Nguyên",
                Distance = route?.Distance ?? 75m,
                EstimatedDuration = route?.EstimatedDuration ?? 90,

                BoardingStopName = ticket.BoardingStop?.StopName ?? "Bến xe Mỹ Đình",
                BoardingStopAddress = ticket.BoardingStop?.Address ?? "Phạm Hùng, Nam Từ Liêm, Hà Nội",
                DepartureTime = trip?.DepartureTime ?? new TimeSpan(7, 0, 0),

                DropOffStopName = ticket.DropOffStop?.StopName ?? "Bến xe Thái Nguyên",
                DropOffStopAddress = ticket.DropOffStop?.Address ?? "Đường Lương Ngọc Quyến, TP Thái Nguyên",
                ArrivalTime = trip?.ArrivalTime ?? new TimeSpan(8, 30, 0),

                TripId = trip?.TripId ?? 1,
                TripDate = trip?.TripDate ?? DateTime.Today,
                LicensePlate = bus?.LicensePlate ?? "20A-12345",
                BusTypeName = bus?.BusType?.TypeName ?? "Xe Limousine VIP 29 chỗ",
                DriverName = driver?.User?.FullName ?? "Nguyễn Văn An",
                DriverPhone = driver?.User?.Phone ?? "0987 654 321",

                QrDataPayload = $"SMARTBUS|TICKET:{ticket.TicketCode}|BOOKING:{ticket.Booking?.BookingCode}|SEAT:{ticket.SeatNumber}|PRICE:{ticket.Price}|DATE:{trip?.TripDate:yyyy-MM-dd}"
            };

            return View(model);
        }
    }
}

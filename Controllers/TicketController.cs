using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models.ViewModels;

namespace WebApplication1.Controllers
{
    public class TicketController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TicketController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Ticket/Detail/{id}
        [HttpGet]
        public IActionResult Detail(string? id, string? status = null)
        {
            var ticketCode = string.IsNullOrEmpty(id) ? "SBG-HN-DN-20251024-008" : id;

            // Truy vấn dữ liệu vé thật từ Database SQL Server
            var ticket = _context.Tickets
                .Include(t => t.Booking)
                    .ThenInclude(b => b.User)
                .Include(t => t.Booking)
                    .ThenInclude(b => b.Trip)
                        .ThenInclude(tr => tr.Route)
                .Include(t => t.Booking)
                    .ThenInclude(b => b.Trip)
                        .ThenInclude(tr => tr.Bus)
                            .ThenInclude(bus => bus.BusType)
                .Include(t => t.Booking)
                    .ThenInclude(b => b.Trip)
                        .ThenInclude(tr => tr.Driver)
                            .ThenInclude(d => d.User)
                .Include(t => t.BoardingStop)
                .Include(t => t.DropOffStop)
                .FirstOrDefault(t => t.TicketCode == ticketCode || t.TicketId.ToString() == id);

            if (ticket != null)
            {
                var trip = ticket.Booking?.Trip;
                var route = trip?.Route;
                var bus = trip?.Bus;
                var driver = trip?.Driver;
                var user = ticket.Booking?.User;

                var departureTimeSpan = trip?.DepartureTime.ToTimeSpan() ?? new TimeSpan(8, 0, 0);
                var arrivalTimeSpan = trip?.ArrivalTime?.ToTimeSpan() ?? departureTimeSpan.Add(TimeSpan.FromHours(2));

                var model = new TicketDetailViewModel
                {
                    TicketId = ticket.TicketId,
                    TicketCode = ticket.TicketCode,
                    SeatNumber = ticket.SeatNumber ?? "VIP-01",
                    Price = ticket.Price,
                    Status = status?.ToUpper() ?? ticket.Status?.ToUpper() ?? "ACTIVE",

                    BookingId = ticket.BookingId,
                    BookingCode = ticket.Booking?.BookingCode ?? "BK-0001",
                    BookingTime = ticket.Booking?.BookingTime ?? DateTime.Now,
                    TotalAmount = ticket.Booking?.TotalAmount ?? ticket.Price,

                    PassengerName = user?.FullName ?? "Hành khách SmartBus",
                    PassengerPhone = user?.Phone ?? "0987654321",
                    PassengerEmail = user?.Email ?? "passenger@smartbus.vn",

                    RouteCode = route?.RouteCode ?? "R01",
                    RouteName = route?.RouteName ?? "Hà Nội - Hải Phòng",
                    StartPoint = route?.StartPoint ?? "Hà Nội",
                    EndPoint = route?.EndPoint ?? "Hải Phòng",
                    Distance = route?.Distance ?? 100,
                    EstimatedDuration = route?.EstimatedDuration ?? 120,

                    BoardingStopName = ticket.BoardingStop?.StopName ?? (route?.StartPoint + " (Bến xe xuất phát)"),
                    BoardingStopAddress = ticket.BoardingStop?.Address ?? "Điểm đón SmartBus",
                    DepartureTime = departureTimeSpan,

                    DropOffStopName = ticket.DropOffStop?.StopName ?? (route?.EndPoint + " (Bến xe trả khách)"),
                    DropOffStopAddress = ticket.DropOffStop?.Address ?? "Điểm trả SmartBus",
                    ArrivalTime = arrivalTimeSpan,

                    TripId = trip?.TripId ?? 1,
                    TripDate = trip?.TripDate.ToDateTime(TimeOnly.MinValue) ?? DateTime.Today,
                    LicensePlate = bus?.LicensePlate ?? "29B-888.88",
                    BusTypeName = bus?.BusType?.TypeName ?? "Xe Limousine Cao Cấp",
                    DriverName = driver?.User?.FullName ?? "Tài xế SmartBus",
                    DriverPhone = driver?.User?.Phone ?? "0988 777 999",

                    QrDataPayload = $"SMARTBUS|TICKET:{ticket.TicketCode}|BOOKING:{ticket.Booking?.BookingCode}|SEAT:{ticket.SeatNumber}|DATE:{trip?.TripDate:yyyy-MM-dd}|HASH:db_synced"
                };

                return View(model);
            }

            // Fallback hiển thị mẫu nếu mã vé không tìm thấy trong DB
            var fallbackModel = new TicketDetailViewModel
            {
                TicketId = 1024,
                TicketCode = ticketCode,
                SeatNumber = "VIP-05",
                Price = 450000m,
                Status = status?.ToUpper() ?? "ACTIVE",
                BookingId = 5082,
                BookingCode = "BK-988214",
                BookingTime = DateTime.Parse("2026-09-28 14:35:00"),
                TotalAmount = 450000m,
                PassengerName = "Nguyễn Văn An",
                PassengerPhone = "0912 345 678",
                PassengerEmail = "nguyenvanan@gmail.com",
                RouteCode = "R08",
                RouteName = "Hà Nội - Đà Nẵng (Cao tốc Bắc Nam)",
                StartPoint = "Bến xe Nước Ngầm, Hà Nội",
                EndPoint = "Bến xe Trung Tâm Đà Nẵng",
                Distance = 760m,
                EstimatedDuration = 750,
                BoardingStopName = "Bến xe Nước Ngầm (Cổng A2)",
                BoardingStopAddress = "Km 8 Giải Phóng, P. Hoàng Liệt, Q. Hoàng Mai, Hà Nội",
                DepartureTime = new TimeSpan(19, 30, 0),
                DropOffStopName = "Bến xe Trung Tâm Đà Nẵng (Cột 04)",
                DropOffStopAddress = "Đường Nam Trân, P. Hòa Minh, Q. Liên Chiểu, Đà Nẵng",
                ArrivalTime = new TimeSpan(8, 0, 0),
                TripId = 302,
                TripDate = DateTime.Today,
                LicensePlate = "29B-888.68",
                BusTypeName = "Limousine VIP 22 Phòng Đơn Cung Điện",
                DriverName = "Trần Đình Trọng (Bằng FC)",
                DriverPhone = "0988 777 999",
                QrDataPayload = $"SMARTBUS|TICKET:{ticketCode}|BOOKING:BK-988214|SEAT:VIP-05|DATE:{DateTime.Today:yyyy-MM-dd}|HASH:mock"
            };

            return View(fallbackModel);
        }
    }
}

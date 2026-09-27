using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models.ViewModels;

namespace WebApplication1.Controllers
{
    public class TicketController : Controller
    {
        // GET: /Ticket/Detail/{id}
        [HttpGet]
        public IActionResult Detail(string? id, string? status = "ACTIVE")
        {
            var ticketCode = string.IsNullOrEmpty(id) ? "SBG-HN-DN-20251024-008" : id;
            var ticketStatus = string.IsNullOrEmpty(status) ? "ACTIVE" : status.ToUpper();

            var model = new TicketDetailViewModel
            {
                TicketId = 1024,
                TicketCode = ticketCode,
                SeatNumber = "VIP-05",
                Price = 450000m,
                Status = ticketStatus,

                BookingId = 5082,
                BookingCode = "BK-988214",
                BookingTime = DateTime.Parse("2025-10-20 14:35:00"),
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
                TripDate = DateTime.Parse("2025-10-24"),
                LicensePlate = "29B-888.68",
                BusTypeName = "Limousine VIP 22 Phòng Đơn Cung Điện",
                DriverName = "Trần Đình Trọng (Bằng FC)",
                DriverPhone = "0988 777 999",

                QrDataPayload = $"SMARTBUS|TICKET:{ticketCode}|BOOKING:BK-988214|SEAT:VIP-05|DATE:2025-10-24|EXP:2025-10-24T23:59:59|HASH:9a7f3c1b"
            };

            return View(model);
        }
    }
}

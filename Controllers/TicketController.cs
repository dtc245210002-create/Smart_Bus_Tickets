using Microsoft.AspNetCore.Mvc;

using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;

using System;
using System.Collections.Generic;
using System.Linq;

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

                SeatNumber = TempData["NewSeatNumber"]?.ToString() ?? "VIP-05",
                Price = TempData["NewTicketPrice"] != null ? Convert.ToDecimal(TempData["NewTicketPrice"]) : 450000m,
                Status = ticketStatus,


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


                BoardingStopName = TempData["NewBoardingStop"]?.ToString() ?? "Bến xe Nước Ngầm (Cổng A2)",
                BoardingStopAddress = "Km 8 Giải Phóng, P. Hoàng Liệt, Q. Hoàng Mai, Hà Nội",
                DepartureTime = TempData["NewDepartureTime"] != null ? TimeSpan.Parse(TempData["NewDepartureTime"]!.ToString()!) : new TimeSpan(19, 30, 0),

                DropOffStopName = TempData["NewDropOffStop"]?.ToString() ?? "Bến xe Trung Tâm Đà Nẵng (Cột 04)",

                DropOffStopAddress = "Đường Nam Trân, P. Hòa Minh, Q. Liên Chiểu, Đà Nẵng",
                ArrivalTime = new TimeSpan(8, 0, 0),
                TripId = 302,

                TripDate = DateTime.Today,
                LicensePlate = "29B-888.68",
                BusTypeName = "Limousine VIP 22 Phòng Đơn Cung Điện",

                TripDate = TempData["NewTripDate"] != null ? DateTime.Parse(TempData["NewTripDate"]!.ToString()!) : DateTime.Parse("2025-10-24"),
                LicensePlate = TempData["NewLicensePlate"]?.ToString() ?? "29B-888.68",
                BusTypeName = TempData["NewBusTypeName"]?.ToString() ?? "Limousine VIP 22 Phòng Đơn Cung Điện",

                DriverName = "Trần Đình Trọng (Bằng FC)",
                DriverPhone = "0988 777 999",
                QrDataPayload = $"SMARTBUS|TICKET:{ticketCode}|BOOKING:BK-988214|SEAT:VIP-05|DATE:{DateTime.Today:yyyy-MM-dd}|HASH:mock"
            };

            return View(fallbackModel);
        }

        // GET: /Ticket/Payment
        [HttpGet]
        public IActionResult Payment()
        {
            return View();
        }

        // =========================================================================
        // LUỒNG ĐỔI CHUYẾN (RESCHEDULE TICKET FLOW - 3 BƯỚC)
        // Bước 1: Tìm kiếm chuyến xe mới (ngày, tuyến xe, điểm đón/trả)
        // Bước 2: Giao diện sơ đồ ghế chuyến mới để khách chọn lại ghế
        // Bước 3: Màn hình so sánh vé cũ và vé mới, tính chênh lệch giá
        // =========================================================================
        [HttpGet]
        public IActionResult Reschedule(
            string? ticketCode = "SBG-HN-DN-20251024-008",
            int step = 1,
            string? newDate = null,
            string? timeOfDay = null,
            int? tripId = null,
            string? newSeat = null,
            decimal? seatPrice = null,
            string? boarding = null,
            string? dropoff = null)
        {
            var oldTicket = GetMockOldTicket(ticketCode ?? "SBG-HN-DN-20251024-008");
            var targetDate = !string.IsNullOrEmpty(newDate) && DateTime.TryParse(newDate, out var parsedDate) 
                ? parsedDate 
                : oldTicket.TripDate.AddDays(2);

            var availableTrips = GetMockRescheduleTrips(targetDate);

            if (!string.IsNullOrEmpty(timeOfDay))
            {
                if (timeOfDay == "morning")
                    availableTrips = availableTrips.Where(t => t.DepartureTime.Hours >= 6 && t.DepartureTime.Hours < 12).ToList();
                else if (timeOfDay == "afternoon")
                    availableTrips = availableTrips.Where(t => t.DepartureTime.Hours >= 12 && t.DepartureTime.Hours < 18).ToList();
                else if (timeOfDay == "evening")
                    availableTrips = availableTrips.Where(t => t.DepartureTime.Hours >= 18 || t.DepartureTime.Hours < 6).ToList();
            }

            TripItemViewModel? selectedTrip = null;
            if (tripId.HasValue)
            {
                selectedTrip = availableTrips.FirstOrDefault(t => t.TripId == tripId.Value) 
                               ?? GetMockRescheduleTrips(targetDate).FirstOrDefault(t => t.TripId == tripId.Value);
            }
            else if (step >= 2 && availableTrips.Any())
            {
                selectedTrip = availableTrips.First();
                tripId = selectedTrip.TripId;
            }

            var model = new RescheduleViewModel
            {
                Step = step,
                OldTicket = oldTicket,
                NewDepartureDate = targetDate,
                TimeOfDayFilter = timeOfDay,
                AvailableTrips = availableTrips,
                SelectedTripId = tripId,
                SelectedTrip = selectedTrip,
                SelectedNewSeat = newSeat ?? (step == 3 ? "VIP-08" : null),
                NewSeatPrice = seatPrice ?? (selectedTrip?.Price ?? 450000m),
                SelectedBoardingPoint = boarding ?? selectedTrip?.BoardingPoints.FirstOrDefault(),
                SelectedDropOffPoint = dropoff ?? selectedTrip?.DropOffPoints.FirstOrDefault()
            };

            return View(model);
        }

        // POST: /Ticket/ConfirmReschedule
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ConfirmReschedule(RescheduleViewModel model)
        {
            // Cập nhật thông tin vé mới vào TempData để chuyển sang trang chi tiết vé
            TempData["SuccessMessage"] = $"Đổi chuyến thành công! Vé {model.OldTicket.TicketCode} đã được cập nhật sang chuyến mới khởi hành ngày {model.NewDepartureDate:dd/MM/yyyy}.";
            TempData["NewSeatNumber"] = model.SelectedNewSeat ?? "VIP-08";
            TempData["NewTicketPrice"] = model.EffectiveNewPrice;
            TempData["NewTripDate"] = model.NewDepartureDate.ToString("yyyy-MM-dd");
            if (model.SelectedTrip != null)
            {
                TempData["NewDepartureTime"] = model.SelectedTrip.DepartureTime.ToString(@"hh\:mm\:ss");
                TempData["NewLicensePlate"] = model.SelectedTrip.LicensePlate;
                TempData["NewBusTypeName"] = model.SelectedTrip.BusTypeName;
            }
            if (!string.IsNullOrEmpty(model.SelectedBoardingPoint))
                TempData["NewBoardingStop"] = model.SelectedBoardingPoint;
            if (!string.IsNullOrEmpty(model.SelectedDropOffPoint))
                TempData["NewDropOffStop"] = model.SelectedDropOffPoint;

            return RedirectToAction("Detail", new { id = model.OldTicket.TicketCode });
        }

        // GET: /Ticket/Cancel/{id}
        [HttpGet]
        public IActionResult Cancel(string? id)
        {
            var ticketCode = string.IsNullOrEmpty(id) ? "SBG-84920" : id;
            var model = GetMockOldTicket(ticketCode);
            return View(model);
        }

        // POST: /Ticket/ConfirmCancel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ConfirmCancel(string ticketCode, string cancelReason, string refundMethod, string? cancelNote)
        {
            TempData["SuccessMessage"] = $"Yêu cầu hủy vé #{ticketCode} thành công! Số tiền hoàn lại 477.000 đ đang được xử lý chuyển về tài khoản của bạn.";
            return RedirectToAction("Detail", new { id = ticketCode, status = "CANCELLED" });
        }

        private TicketDetailViewModel GetMockOldTicket(string ticketCode)
        {
            return new TicketDetailViewModel
            {
                TicketId = 1024,
                TicketCode = ticketCode,
                SeatNumber = "VIP-05",
                Price = 450000m,
                Status = "ACTIVE",
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
        }

        private List<TripItemViewModel> GetMockRescheduleTrips(DateTime tripDate)
        {
            var trips = new List<TripItemViewModel>();

            // Chuyến 1: Buổi sáng - Limousine 34 Phòng - Giá 400.000đ (RẺ HƠN vé cũ 50.000đ -> Hoàn tiền)
            trips.Add(new TripItemViewModel
            {
                TripId = 201,
                OperatorName = "Vân Khôi Limousine",
                Rating = 4.8,
                ReviewCount = 890,
                BusTypeName = "Limousine VIP 34 Phòng Giường Nằm",
                BusImage = "https://images.unsplash.com/photo-1544620347-c4fd4a3d5957?w=400&q=80",
                LicensePlate = "29B-777.22",
                DepartureTime = new TimeSpan(8, 30, 0),
                DeparturePoint = "Bến xe Nước Ngầm (Cổng A1)",
                ArrivalTime = new TimeSpan(21, 0, 0),
                ArrivalPoint = "Bến xe Trung Tâm Đà Nẵng",
                DurationText = "12h 30m",
                Price = 400000m,
                OriginalPrice = 450000m,
                AvailableSeats = 14,
                TotalCapacity = 34,
                IsFlashSale = true,
                FlashSaleText = "TIẾT KIỆM 50K",
                NoticeText = "Khởi hành buổi sáng, ngắm cảnh đường mòn Hồ Chí Minh & Đèo Hải Vân",
                BoardingPoints = new List<string> { "Bến xe Nước Ngầm (08:30)", "Cổng Trường ĐH Xây Dựng (08:50)", "Trạm Thu Phí Pháp Vân (09:15)" },
                DropOffPoints = new List<string> { "Ngã 3 Huế, Đà Nẵng (20:30)", "Bến xe Trung Tâm Đà Nẵng (21:00)" },
                Seats = GenerateRescheduleSeats(34, new[] { "A01", "A04", "B02", "B05" }, 400000m)
            });

            // Chuyến 2: Buổi chiều - Limousine 22 Phòng Cung Điện - Giá 450.000đ (BẰNG GIÁ vé cũ -> Đổi miễn phí 0đ)
            trips.Add(new TripItemViewModel
            {
                TripId = 202,
                OperatorName = "Hoàng Long Asia Express",
                Rating = 4.9,
                ReviewCount = 1420,
                BusTypeName = "Limousine VIP 22 Phòng Đơn Cung Điện",
                BusImage = "https://images.unsplash.com/photo-1570125909232-eb263c188f7e?w=400&q=80",
                LicensePlate = "29B-888.68",
                DepartureTime = new TimeSpan(14, 0, 0),
                DeparturePoint = "Bến xe Giáp Bát (Cửa số 2)",
                ArrivalTime = new TimeSpan(2, 30, 0),
                ArrivalPoint = "Bến xe Trung Tâm Đà Nẵng",
                DurationText = "12h 30m",
                Price = 450000m,
                OriginalPrice = 480000m,
                AvailableSeats = 8,
                TotalCapacity = 22,
                IsFlashSale = false,
                NoticeText = "Bằng giá với vé cũ của bạn. Đổi chuyến hoàn toàn miễn phí.",
                BoardingPoints = new List<string> { "Bến xe Giáp Bát (14:00)", "Bến xe Nước Ngầm (14:25)", "Nút Giao Liêm Tuyền (15:10)" },
                DropOffPoints = new List<string> { "Cầu Thuận Phước (02:15)", "Bến xe Trung Tâm Đà Nẵng (02:30)" },
                Seats = GenerateRescheduleSeats(22, new[] { "A01", "A03", "B01", "B04" }, 450000m)
            });

            // Chuyến 3: Buổi tối - Royal Luxury Suite 20 Phòng - Giá 520.000đ (ĐẮT HƠN vé cũ 70.000đ -> Bù thêm tiền)
            trips.Add(new TripItemViewModel
            {
                TripId = 203,
                OperatorName = "SmartBus Royal Luxury",
                Rating = 5.0,
                ReviewCount = 2680,
                BusTypeName = "Royal Luxury 20 Phòng VIP (Massage, Màn hình 4K)",
                BusImage = "https://images.unsplash.com/photo-1494515843206-f3117d3f51b7?w=400&q=80",
                LicensePlate = "29B-999.88",
                DepartureTime = new TimeSpan(20, 30, 0),
                DeparturePoint = "Nhà Hát Lớn Hà Nội (Cổng VIP)",
                ArrivalTime = new TimeSpan(8, 45, 0),
                ArrivalPoint = "Bến xe Trung Tâm Đà Nẵng (Có trung chuyển tận nơi)",
                DurationText = "12h 15m",
                Price = 520000m,
                OriginalPrice = 560000m,
                AvailableSeats = 6,
                TotalCapacity = 20,
                IsFlashSale = true,
                FlashSaleText = "HẠNG THƯƠNG GIA",
                NoticeText = "Nâng cấp lên xe phòng thương gia cao cấp, phục vụ bữa ăn nhẹ & nước uống miễn phí",
                BoardingPoints = new List<string> { "Nhà Hát Lớn Hà Nội (20:30)", "Bến xe Nước Ngầm (21:00)" },
                DropOffPoints = new List<string> { "Cầu Rồng Đà Nẵng (08:20)", "Bến xe Trung Tâm Đà Nẵng (08:45)", "Trả khách tận khách sạn nội thành" },
                Seats = GenerateRescheduleSeats(20, new[] { "A01", "A02", "B03" }, 520000m)
            });

            return trips;
        }

        private List<SeatItemViewModel> GenerateRescheduleSeats(int capacity, string[] bookedSeats, decimal basePrice)
        {
            var seats = new List<SeatItemViewModel>();
            var rows = (capacity + 1) / 2;

            for (int r = 1; r <= rows; r++)
            {
                foreach (var col in new[] { "VIP", "T" })
                {
                    var seatCode = $"{col}-{r:D2}";
                    if (seats.Count >= capacity) break;

                    seats.Add(new SeatItemViewModel
                    {
                        SeatCode = seatCode,
                        Floor = r <= (rows / 2) ? 1 : 2,
                        Price = basePrice,
                        Status = bookedSeats.Contains(seatCode) ? "BOOKED" : "AVAILABLE"
                    });
                }
            }
            return seats;
        }
    }
}

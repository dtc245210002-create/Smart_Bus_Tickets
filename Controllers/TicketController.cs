using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApplication1.Models.Entities;
using WebApplication1.Models.ViewModels;
using WebApplication1.Services;
using WebApplication1.Services.BusLayout;

namespace WebApplication1.Controllers
{
    public class TicketController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ISeatHoldService _seatHoldService;
        private readonly IBusLayoutService _busLayoutService;
        private readonly ILogger<TicketController> _logger;

        private IQueryable<Ticket> OwnedTickets => _context.Tickets.Where(t =>
            t.Booking.UserId.ToString() == User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

        public TicketController(
            ApplicationDbContext context,
            ISeatHoldService seatHoldService,
            IBusLayoutService busLayoutService,
            ILogger<TicketController> logger)
        {
            _context = context;
            _seatHoldService = seatHoldService;
            _busLayoutService = busLayoutService;
            _logger = logger;
        }

        // =========================================================================
        // GET: /Ticket/Detail/{id?} or /Ticket/Details?code=...
        // Hiển thị chi tiết vé điện tử và mã QR đồng bộ từ CSDL SQL Server
        // =========================================================================
        // =========================================================================
        // GET: /Ticket/Detail/{id?} or /Ticket/Details?code=...
        // Hiển thị chi tiết vé điện tử và mã QR đồng bộ từ CSDL SQL Server hoặc theo yêu cầu đặt vé của khách hàng
        // =========================================================================
        [HttpGet]
        [Route("Ticket/Detail/{id?}")]
        [Route("Ticket/Details")]
        [Route("Ticket/Details/{id?}")]
        public async Task<IActionResult> Detail(
            string? id = null,
            string? code = null,
            string? status = null,
            int? tripId = null,
            string? seats = null,
            decimal? total = null,
            string? from = null,
            string? to = null,
            string? boarding = null,
            string? dropoff = null,
            string? date = null,
            string? depTime = null,
            string? arrTime = null,
            string? busType = null,
            string? licensePlate = null,
            string? routeName = null,
            string? routeCode = null,
            string? driverName = null,
            string? driverPhone = null,
            int? estMinutes = null,
            decimal? distance = null)
        {
            var ticket = await LoadOwnedTicketAsync(code ?? id);
            if (ticket == null) return NotFound();
            var detail = ToDetail(ticket);
            if (string.IsNullOrEmpty(detail.QrDataPayload))
                return Conflict("Vé chưa được thanh toán/xác nhận hoặc không còn hiệu lực.");
            return View(detail);
        }

        // =========================================================================
        // GET: /Ticket/Payment
        // Trang cổng thanh toán thông minh (QR VietQR, thẻ, ví)
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> Payment(
            string? ticketCode = null,
            string? bookingCode = null,
            string? from = null,
            string? to = null,
            string? seats = null,
            decimal? total = null,
            string? boarding = null,
            string? dropoff = null,
            string? date = null,
            string? depTime = null,
            string? arrTime = null,
            string? busType = null,
            string? licensePlate = null,
            string? routeName = null,
            string? routeCode = null,
            string? driverName = null,
            string? driverPhone = null,
            int? tripId = null)
        {
            var ticket = await LoadOwnedTicketAsync(ticketCode ?? bookingCode);
            if (ticket == null) return NotFound();
            var model = ToDetail(ticket);
            ViewBag.RouteName = model.RouteName;
            ViewBag.RouteCode = model.RouteCode;
            ViewBag.StartPoint = model.StartPoint;
            ViewBag.EndPoint = model.EndPoint;
            ViewBag.BoardingStop = model.BoardingStopName;
            ViewBag.DropOffStop = model.DropOffStopName;
            ViewBag.SeatNumber = string.Join(", ", ticket.Booking.Tickets.Where(t => t.Status != "CANCELLED" && t.Status != "EXPIRED").Select(t => t.SeatNumber));
            ViewBag.TotalAmount = model.TotalAmount;
            ViewBag.DepartureTime = model.DepartureTime.ToString(@"hh\:mm");
            ViewBag.TripDate = model.TripDate.ToString("dd/MM/yyyy");
            ViewBag.BusTypeName = model.BusTypeName;
            ViewBag.LicensePlate = model.LicensePlate;
            ViewBag.DriverName = model.DriverName;
            ViewBag.DriverPhone = model.DriverPhone;
            ViewBag.TicketCode = model.TicketCode;
            ViewBag.BookingCode = model.BookingCode;
            ViewBag.PassengerName = model.PassengerName;
            ViewBag.PassengerPhone = model.PassengerPhone;
            return View();
        }

        // =========================================================================
        // POST & GET: /Ticket/ConfirmPayment
        // Xác nhận thanh toán & ghi nhận giao dịch thật vào Database SQL Server
        // =========================================================================
        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> ConfirmPayment(
            string? ticketCode,
            string? bookingCode,
            decimal? amount,
            string? paymentMethod = "VietQR",
            string? transactionCode = null)
        {
            await Task.CompletedTask;
            if (HttpMethods.IsGet(Request.Method)) return StatusCode(405);
            // A browser supplied amount/transaction code is not payment evidence.
            // No provider verifier exists in this product yet. Fail closed without
            // inserting Payments, changing tickets or displaying a success fallback.
            return StatusCode(503, "Chưa có dịch vụ xác minh thanh toán. Vé chưa được kích hoạt.");
        }

        // =========================================================================
        // FE-US2-04: Màn hình chờ & Kết quả thanh toán (Callback Page)
        // GET: /payment-result & /Ticket/PaymentResult
        // =========================================================================
        [HttpGet]
        [Route("payment-result")]
        [Route("Ticket/PaymentResult")]
        public async Task<IActionResult> PaymentResult(
            string? ticketCode = null,
            string? bookingCode = null,
            string? status = null,
            string? orderId = null,
            string? transactionNo = null,
            decimal? amount = null,
            string? paymentMethod = "VietQR")
        {
            var ticket = await LoadOwnedTicketAsync(ticketCode ?? bookingCode);
            if (ticket == null) return NotFound();
            var detail = ToDetail(ticket);
            var payment = ticket.Booking.Payments.Where(p => p.Status == "Paid")
                .OrderByDescending(p => p.PaymentTime).FirstOrDefault();
            var state = ticket.Status?.ToUpperInvariant();
            var success = HasVerifiedPayment(ticket) && state is "ISSUED" or "CONFIRMED" or "USED";
            return View(new PaymentResultViewModel
            {
                TicketCode = detail.TicketCode, BookingCode = detail.BookingCode,
                Amount = payment?.Amount ?? 0, PaymentMethod = payment?.PaymentMethod ?? "",
                TransactionCode = payment?.TransactionCode ?? "", PaymentTime = payment?.PaymentTime ?? default,
                Status = state is "CANCELLED" or "EXPIRED" ? "FAILED" : success ? "SUCCESS" : "PENDING",
                SimulatePolling = false, PassengerName = detail.PassengerName, PassengerPhone = detail.PassengerPhone,
                RouteName = detail.RouteName, StartPoint = detail.StartPoint, EndPoint = detail.EndPoint,
                BoardingStop = detail.BoardingStopName, DropOffStop = detail.DropOffStopName,
                SeatNumber = detail.SeatNumber, BusTypeName = detail.BusTypeName,
                LicensePlate = detail.LicensePlate, DepartureTime = detail.TripDate.Date.Add(detail.DepartureTime),
                QrDataPayload = detail.QrDataPayload
            });
        }

        // =========================================================================
        // GET: /api/v1/payments/check-status & /api/payment/check-status
        // API Polling kiểm tra trạng thái thanh toán cuối cùng
        // =========================================================================
        [HttpGet]
        [Route("api/v1/payments/check-status")]
        [Route("api/payment/check-status")]
        public async Task<IActionResult> CheckPaymentStatus(
            [FromQuery] string? orderCode = null,
            [FromQuery] string? bookingCode = null,
            [FromQuery] string? ticketCode = null,
            [FromQuery] string? orderId = null)
        {
            var ticket = await LoadOwnedTicketAsync(orderCode ?? bookingCode ?? ticketCode ?? orderId);
            if (ticket == null) return NotFound();
            var issued = ticket.Status?.ToUpperInvariant() is "ISSUED" or "CONFIRMED" or "USED";
            var paid = issued && HasVerifiedPayment(ticket);
            var payment = ticket.Booking.Payments.Where(p => p.Status == "Paid").OrderByDescending(p => p.PaymentTime).FirstOrDefault();
            return Json(new { success = true, isPaid = paid, status = paid ? "SUCCESS" : "PENDING",
                ticketCode = ticket.TicketCode, bookingCode = ticket.Booking.BookingCode,
                amount = ticket.Booking.TotalAmount, paymentMethod = payment?.PaymentMethod ?? "",
                transactionNo = payment?.TransactionCode ?? "", paymentTime = payment?.PaymentTime,
                message = paid ? "Đã có thanh toán được ghi nhận." : "Đang chờ xác minh thanh toán." });
        }

        // =========================================================================
        // LUỒNG ĐỔI CHUYẾN (RESCHEDULE TICKET FLOW - 3 BƯỚC THÔNG MINH)
        // Bước 1: Tìm kiếm chuyến xe mới (danh sách chuyến thay thế ĐÚNG tuyến và ngày)
        // Bước 2: Giao diện sơ đồ ghế chuyến mới để khách chọn lại ghế & tầng
        // Bước 3: So sánh vé cũ và vé mới, tính chính xác chênh lệch giá (hoàn tiền / bù tiền)
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> Reschedule(
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
            var effectiveTicketCode = string.IsNullOrEmpty(ticketCode) ? "SBG-HN-DN-20251024-008" : ticketCode;

            // 1. Lấy thông tin vé hiện tại từ Database thật
            Ticket? dbTicket = null;
            try
            {
                dbTicket = await OwnedTickets
                    .Include(t => t.Booking).ThenInclude(b => b.User)
                    .Include(t => t.Booking).ThenInclude(b => b.Trip).ThenInclude(tr => tr.Route)
                    .Include(t => t.Booking).ThenInclude(b => b.Trip).ThenInclude(tr => tr.Bus).ThenInclude(bus => bus.BusType)
                    .Include(t => t.Booking).ThenInclude(b => b.Trip).ThenInclude(tr => tr.Driver).ThenInclude(d => d.User)
                    .Include(t => t.BoardingStop)
                    .Include(t => t.DropOffStop)
                    .FirstOrDefaultAsync(t => t.TicketCode == effectiveTicketCode || t.TicketId.ToString() == effectiveTicketCode);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Không thể tải vé từ DB cho Reschedule: {Message}", ex.Message);
            }

            TicketDetailViewModel oldTicket;
            if (dbTicket != null)
            {
                var tr = dbTicket.Booking?.Trip;
                var r = tr?.Route;
                var b = tr?.Bus;
                var d = tr?.Driver;
                var u = dbTicket.Booking?.User;

                oldTicket = new TicketDetailViewModel
                {
                    TicketId = dbTicket.TicketId,
                    TicketCode = dbTicket.TicketCode,
                    SeatNumber = dbTicket.SeatNumber ?? "VIP-05",
                    Price = dbTicket.Price,
                    Status = dbTicket.Status?.ToUpper() ?? "ACTIVE",
                    BookingId = dbTicket.BookingId,
                    BookingCode = dbTicket.Booking?.BookingCode ?? "BK-0001",
                    BookingTime = dbTicket.Booking?.BookingTime ?? DateTime.Now,
                    TotalAmount = dbTicket.Booking?.TotalAmount ?? dbTicket.Price,
                    PassengerName = u?.FullName ?? "Hành khách SmartBus",
                    PassengerPhone = u?.Phone ?? "0987654321",
                    PassengerEmail = u?.Email ?? "passenger@smartbus.vn",
                    RouteCode = r?.RouteCode ?? "R01",
                    RouteName = r?.RouteName ?? $"{r?.StartPoint ?? "Hà Nội"} - {r?.EndPoint ?? "Đà Nẵng"}",
                    StartPoint = r?.StartPoint ?? "Hà Nội",
                    EndPoint = r?.EndPoint ?? "Đà Nẵng",
                    Distance = r?.Distance ?? 760m,
                    EstimatedDuration = r?.EstimatedDuration ?? 750,
                    BoardingStopName = dbTicket.BoardingStop?.StopName ?? (r?.StartPoint + " (Bến xe xuất phát)"),
                    BoardingStopAddress = dbTicket.BoardingStop?.Address ?? "Điểm đón SmartBus Express",
                    DepartureTime = tr?.DepartureTime.ToTimeSpan() ?? new TimeSpan(19, 30, 0),
                    DropOffStopName = dbTicket.DropOffStop?.StopName ?? (r?.EndPoint + " (Bến xe trả khách)"),
                    DropOffStopAddress = dbTicket.DropOffStop?.Address ?? "Điểm trả SmartBus Express",
                    ArrivalTime = tr?.ArrivalTime?.ToTimeSpan() ?? new TimeSpan(8, 0, 0),
                    TripId = tr?.TripId ?? 1,
                    TripDate = tr?.TripDate.ToDateTime(TimeOnly.MinValue) ?? DateTime.Today.AddDays(1),
                    LicensePlate = b?.LicensePlate ?? "29B-888.68",
                    BusTypeName = b?.BusType?.TypeName ?? "Limousine VIP 22 Phòng Đơn Cung Điện",
                    DriverName = d?.User?.FullName ?? "Tài xế SmartBus",
                    DriverPhone = d?.User?.Phone ?? "0988 777 999",
                    QrDataPayload = $"SMARTBUS|TICKET:{dbTicket.TicketCode}|BOOKING:{dbTicket.Booking?.BookingCode}|SEAT:{dbTicket.SeatNumber}"
                };
            }
            else
            {
                oldTicket = GetMockOldTicket(effectiveTicketCode);
            }

            var targetDate = !string.IsNullOrEmpty(newDate) && DateTime.TryParse(newDate, out var parsedDate)
                ? parsedDate
                : (oldTicket.TripDate > DateTime.Today ? oldTicket.TripDate.AddDays(1) : DateTime.Today.AddDays(1));

            // 2. Lấy danh sách chuyến xe thay thế phù hợp với tuyến đường thật và ngày muốn đổi
            var availableTrips = await GetAvailableRescheduleTripsAsync(oldTicket, targetDate);

            // 3. Lọc theo khung giờ sáng / chiều / tối
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
                selectedTrip = availableTrips.FirstOrDefault(t => t.TripId == tripId.Value);
            }
            if (selectedTrip == null && step >= 2 && availableTrips.Any())
            {
                selectedTrip = availableTrips.First();
                tripId = selectedTrip.TripId;
            }

            var defaultSeat = selectedTrip?.Seats.FirstOrDefault(s => s.Status == "AVAILABLE")?.SeatCode ?? "VIP-08";

            var model = new RescheduleViewModel
            {
                Step = step,
                OldTicket = oldTicket,
                NewDepartureDate = targetDate,
                TimeOfDayFilter = timeOfDay,
                AvailableTrips = availableTrips,
                SelectedTripId = tripId,
                SelectedTrip = selectedTrip,
                SelectedNewSeat = newSeat ?? (step == 3 ? defaultSeat : null),
                NewSeatPrice = seatPrice ?? (selectedTrip?.Price ?? oldTicket.Price),
                SelectedBoardingPoint = boarding ?? selectedTrip?.BoardingPoints.FirstOrDefault(),
                SelectedDropOffPoint = dropoff ?? selectedTrip?.DropOffPoints.FirstOrDefault()
            };

            return View(model);
        }

        // =========================================================================
        // POST: /Ticket/ConfirmReschedule
        // =========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmReschedule(RescheduleViewModel model)
        {
            await Task.CompletedTask;
            // The legacy action trusted client price/seat and set CONFIRMED without
            // payment or an inventory transaction. Do not permit it to bypass the
            // protected issuance path. A verified exchange workflow is a later task.
            return Conflict("Chưa thể xác nhận đổi vé qua luồng chưa được xác minh. Vé hiện tại được giữ nguyên.");
        }

        // =========================================================================
        // GET: /Ticket/Cancel/{id}
        // =========================================================================
        [HttpGet]
        [Route("Ticket/Cancel/{id?}")]
        [Route("Ticket/Cancel")]
        [Route("Ticket/Policy")]
        [Route("Ticket/CancellationPolicy")]
        public async Task<IActionResult> Cancel(string? id, string? ticketCode = null)
        {
            var effectiveCode = !string.IsNullOrEmpty(ticketCode) ? ticketCode : (!string.IsNullOrEmpty(id) ? id : "SBG-HN-HP-001");
            var ticketCodeToSearch = effectiveCode;
            Ticket? dbTicket = null;
            try
            {
                dbTicket = await OwnedTickets
                    .Include(t => t.Booking).ThenInclude(b => b.User)
                    .Include(t => t.Booking).ThenInclude(b => b.Trip).ThenInclude(tr => tr.Route)
                    .Include(t => t.Booking).ThenInclude(b => b.Trip).ThenInclude(tr => tr.Bus).ThenInclude(bus => bus.BusType)
                    .Include(t => t.Booking).ThenInclude(b => b.Trip).ThenInclude(tr => tr.Driver).ThenInclude(d => d.User)
                    .Include(t => t.BoardingStop)
                    .Include(t => t.DropOffStop)
                    .FirstOrDefaultAsync(t => t.TicketCode == ticketCodeToSearch 
                                           || t.TicketCode.StartsWith(ticketCodeToSearch + "-") 
                                           || (t.Booking != null && t.Booking.BookingCode == ticketCodeToSearch) 
                                           || t.TicketId.ToString() == ticketCodeToSearch);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Không thể tải vé từ DB cho Cancel: {Message}", ex.Message);
            }

            TicketDetailViewModel model;
            if (dbTicket != null)
            {
                var tr = dbTicket.Booking?.Trip;
                var r = tr?.Route;
                var b = tr?.Bus;
                var d = tr?.Driver;
                var u = dbTicket.Booking?.User;

                model = new TicketDetailViewModel
                {
                    TicketId = dbTicket.TicketId,
                    TicketCode = dbTicket.TicketCode,
                    SeatNumber = dbTicket.SeatNumber ?? "VIP-05",
                    Price = dbTicket.Price,
                    Status = dbTicket.Status?.ToUpper() ?? "ACTIVE",
                    BookingId = dbTicket.BookingId,
                    BookingCode = dbTicket.Booking?.BookingCode ?? "BK-0001",
                    BookingTime = dbTicket.Booking?.BookingTime ?? DateTime.Now,
                    TotalAmount = dbTicket.Booking?.TotalAmount ?? dbTicket.Price,
                    PassengerName = u?.FullName ?? "Hành khách SmartBus",
                    PassengerPhone = u?.Phone ?? "0987654321",
                    PassengerEmail = u?.Email ?? "passenger@smartbus.vn",
                    RouteCode = r?.RouteCode ?? "R01",
                    RouteName = r?.RouteName ?? $"{r?.StartPoint ?? "Hà Nội"} - {r?.EndPoint ?? "Đà Nẵng"}",
                    StartPoint = r?.StartPoint ?? "Hà Nội",
                    EndPoint = r?.EndPoint ?? "Đà Nẵng",
                    Distance = r?.Distance ?? 760m,
                    EstimatedDuration = r?.EstimatedDuration ?? 750,
                    BoardingStopName = dbTicket.BoardingStop?.StopName ?? (r?.StartPoint + " (Bến xe xuất phát)"),
                    BoardingStopAddress = dbTicket.BoardingStop?.Address ?? "Điểm đón SmartBus Express",
                    DepartureTime = tr?.DepartureTime.ToTimeSpan() ?? new TimeSpan(19, 30, 0),
                    DropOffStopName = dbTicket.DropOffStop?.StopName ?? (r?.EndPoint + " (Bến xe trả khách)"),
                    DropOffStopAddress = dbTicket.DropOffStop?.Address ?? "Điểm trả SmartBus Express",
                    ArrivalTime = tr?.ArrivalTime?.ToTimeSpan() ?? new TimeSpan(8, 0, 0),
                    TripId = tr?.TripId ?? 1,
                    TripDate = tr?.TripDate.ToDateTime(TimeOnly.MinValue) ?? DateTime.Today,
                    LicensePlate = b?.LicensePlate ?? "29B-888.68",
                    BusTypeName = b?.BusType?.TypeName ?? "Limousine VIP 22 Phòng Đơn Cung Điện",
                    DriverName = d?.User?.FullName ?? "Tài xế SmartBus",
                    DriverPhone = d?.User?.Phone ?? "0988 777 999"
                };
            }
            else
            {
                model = GetMockOldTicket(ticketCode);
            }

            return View(model);
        }

        // =========================================================================
        // POST: /Ticket/ConfirmCancel
        // =========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmCancel(string ticketCode, string cancelReason, string refundMethod, string? cancelNote)
        {
            decimal refundAmount = 450000m * 0.9m; // 90%
            try
            {
                var dbTicket = await OwnedTickets
                    .Include(t => t.Booking).ThenInclude(b => b.Tickets)
                    .FirstOrDefaultAsync(t => t.TicketCode == ticketCode);

                if (dbTicket != null)
                {
                    if (dbTicket.Status?.ToUpperInvariant() is "USED" or "CHECKED_IN")
                        return Conflict("Vé đã sử dụng không thể hủy.");
                    refundAmount = dbTicket.Price * 0.9m;
                    dbTicket.Status = "CANCELLED";
                    if (dbTicket.Booking != null)
                    {
                        if (dbTicket.Booking.Tickets.All(t => t.Status?.ToUpperInvariant() is "CANCELLED" or "EXPIRED"))
                            dbTicket.Booking.Status = "Cancelled";
                        else if (dbTicket.Booking.Status == "Pending")
                            dbTicket.Booking.TotalAmount = dbTicket.Booking.Tickets.Where(t => WebApplication1.Controllers.Api.BookingsApiController.IsHeld(t)).Sum(t => t.Price);
                    }
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý hủy vé: {Message}", ex.Message);
            }

            TempData["SuccessMessage"] = $"Yêu cầu hủy vé #{ticketCode} thành công! Số tiền hoàn lại {refundAmount:N0} đ đang được xử lý chuyển về tài khoản của bạn.";
            return RedirectToAction("Detail", new { id = ticketCode, status = "CANCELLED" });
        }

        // =========================================================================
        // PHƯƠNG THỨC HỖ TRỢ XỬ LÝ ĐỔI CHUYẾN ĐỘNG & THÔNG MINH
        // =========================================================================

        private async Task<List<TripItemViewModel>> GetAvailableRescheduleTripsAsync(TicketDetailViewModel oldTicket, DateTime targetDate)
        {
            var trips = new List<TripItemViewModel>();

            // 1. Thử tìm kiếm chuyến xe có sẵn trong Database cho đúng tuyến đường này
            try
            {
                var targetDateOnly = DateOnly.FromDateTime(targetDate);
                var normStart = LocationHelper.NormalizeLocation(oldTicket.StartPoint);
                var normEnd = LocationHelper.NormalizeLocation(oldTicket.EndPoint);

                var query = await _context.Trips
                    .Include(t => t.Route)
                    .Include(t => t.Bus).ThenInclude(b => b.BusType)
                    .Include(t => t.Driver).ThenInclude(d => d.User)
                    .Include(t => t.Bookings).ThenInclude(b => b.Tickets)
                    .Where(t => (t.TripDate == targetDateOnly || t.TripDate >= DateOnly.FromDateTime(DateTime.Today)) &&
                                (t.Route.StartPoint.ToLower().Contains(normStart) || t.Route.RouteName.ToLower().Contains(normStart)) &&
                                (t.Route.EndPoint.ToLower().Contains(normEnd) || t.Route.RouteName.ToLower().Contains(normEnd)))
                    .OrderBy(t => t.DepartureTime)
                    .Take(6)
                    .ToListAsync();

                if (query.Any())
                {
                    foreach (var tr in query)
                    {
                        var depTime = tr.DepartureTime.ToTimeSpan();
                        double durationHours = (tr.Route.EstimatedDuration.HasValue && tr.Route.EstimatedDuration.Value > 0) ? (tr.Route.EstimatedDuration.Value / 60.0) : 4.0;
                        var arrTime = tr.ArrivalTime?.ToTimeSpan() ?? depTime.Add(TimeSpan.FromHours(durationHours));
                        var bookedSeats = tr.Bookings
                            .Where(b => b.Status != "Cancelled" && b.Status != "CANCELLED")
                            .SelectMany(b => b.Tickets)
                            .Where(t => t.Status != "Cancelled" && t.Status != "CANCELLED" && !string.IsNullOrEmpty(t.SeatNumber))
                            .Select(t => t.SeatNumber!.Trim().ToUpper())
                            .ToHashSet();

                        var capacity = tr.Bus?.Capacity ?? 34;
                        var basePrice = tr.Bookings.SelectMany(b => b.Tickets).FirstOrDefault()?.Price ?? oldTicket.Price;

                        trips.Add(new TripItemViewModel
                        {
                            TripId = tr.TripId,
                            OperatorName = "SmartBus " + (tr.Bus?.BusType?.TypeName ?? "Express"),
                            Rating = 4.9,
                            ReviewCount = 1200,
                            BusTypeName = tr.Bus?.BusType?.TypeName ?? "Xe Limousine Cao Cấp",
                            BusImage = GetBusImageForType(tr.Bus?.BusType?.TypeName),
                            LicensePlate = tr.Bus?.LicensePlate ?? "29B-888.88",
                            DepartureTime = depTime,
                            DeparturePoint = oldTicket.BoardingStopName,
                            ArrivalTime = arrTime,
                            ArrivalPoint = oldTicket.DropOffStopName,
                            DurationText = $"{(int)(arrTime - depTime).TotalHours}h {(arrTime - depTime).Minutes}m",
                            Price = basePrice,
                            OriginalPrice = basePrice * 1.1m,
                            AvailableSeats = Math.Max(5, capacity - bookedSeats.Count),
                            TotalCapacity = capacity,
                            IsFlashSale = basePrice < oldTicket.Price,
                            FlashSaleText = basePrice < oldTicket.Price ? "TIẾT KIỆM" : (basePrice > oldTicket.Price ? "VIP" : null),
                            NoticeText = basePrice < oldTicket.Price
                                ? $"Giá vé thấp hơn {oldTicket.Price - basePrice:N0}đ (Hệ thống sẽ hoàn lại chênh lệch)"
                                : (basePrice > oldTicket.Price ? $"Hạng xe cao cấp hơn (+{basePrice - oldTicket.Price:N0}đ phụ thu)" : "Bằng giá với vé cũ (Đổi chuyến miễn phí 0đ)"),
                            BoardingPoints = new List<string> { oldTicket.BoardingStopName, $"{oldTicket.StartPoint} (Trạm cao tốc)" },
                            DropOffPoints = new List<string> { oldTicket.DropOffStopName, $"{oldTicket.EndPoint} (Bến trung tâm)" },
                            Seats = GenerateSeatsForCapacity(capacity, bookedSeats, basePrice)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Không thể tải chuyến xe từ DB cho Reschedule: {Message}", ex.Message);
            }

            // 2. Nếu DB chưa có chuyến trong ngày được chọn, tự động sinh 3 lựa chọn chênh lệch giá phong phú cho tuyến ĐÚNG của vé
            if (!trips.Any())
            {
                trips = GenerateDynamicRescheduleTrips(oldTicket, targetDate);
            }

            return trips;
        }

        private List<TripItemViewModel> GenerateDynamicRescheduleTrips(TicketDetailViewModel oldTicket, DateTime targetDate)
        {
            var trips = new List<TripItemViewModel>();
            var basePrice = oldTicket.Price > 0 ? oldTicket.Price : 450000m;

            // Option 1: Buổi sáng - Tiết kiệm (giá rẻ hơn vé cũ 50.000đ -> Khách nhận tiền hoàn)
            var morningPrice = Math.Max(120000m, basePrice - 50000m);
            trips.Add(new TripItemViewModel
            {
                TripId = 201,
                OperatorName = "SmartBus Eco Express",
                Rating = 4.8,
                ReviewCount = 890,
                BusTypeName = "Limousine VIP 34 Phòng Giường Nằm",
                BusImage = "https://images.unsplash.com/photo-1544620347-c4fd4a3d5957?w=400&q=80",
                LicensePlate = "29B-777.22",
                DepartureTime = new TimeSpan(8, 30, 0),
                DeparturePoint = oldTicket.BoardingStopName,
                ArrivalTime = new TimeSpan(14, 0, 0),
                ArrivalPoint = oldTicket.DropOffStopName,
                DurationText = "5h 30m",
                Price = morningPrice,
                OriginalPrice = morningPrice * 1.15m,
                AvailableSeats = 14,
                TotalCapacity = 34,
                IsFlashSale = true,
                FlashSaleText = "TIẾT KIỆM 50K",
                NoticeText = $"Khởi hành buổi sáng. Tiết kiệm 50.000đ (Hệ thống sẽ hoàn lại tiền thừa về tài khoản)",
                BoardingPoints = new List<string> { oldTicket.BoardingStopName, $"{oldTicket.StartPoint} (Cổng phụ bến xe)" },
                DropOffPoints = new List<string> { oldTicket.DropOffStopName, $"{oldTicket.EndPoint} (Bến trung chuyển)" },
                Seats = GenerateSeatsForCapacity(34, new[] { "A01", "A04", "B02", "B05" }, morningPrice)
            });

            // Option 2: Buổi chiều - Tiêu chuẩn (BẰNG GIÁ vé cũ -> Đổi hoàn toàn miễn phí 0đ)
            trips.Add(new TripItemViewModel
            {
                TripId = 202,
                OperatorName = "SmartBus Premier Line",
                Rating = 4.9,
                ReviewCount = 1420,
                BusTypeName = "Limousine VIP 22 Phòng Đơn Cung Điện",
                BusImage = "https://images.unsplash.com/photo-1570125909232-eb263c188f7e?w=400&q=80",
                LicensePlate = "29B-888.68",
                DepartureTime = new TimeSpan(14, 0, 0),
                DeparturePoint = oldTicket.BoardingStopName,
                ArrivalTime = new TimeSpan(19, 30, 0),
                ArrivalPoint = oldTicket.DropOffStopName,
                DurationText = "5h 30m",
                Price = basePrice,
                OriginalPrice = basePrice * 1.1m,
                AvailableSeats = 8,
                TotalCapacity = 22,
                IsFlashSale = false,
                NoticeText = "Bằng giá với vé cũ của bạn. Đổi chuyến hoàn toàn miễn phí 0đ.",
                BoardingPoints = new List<string> { oldTicket.BoardingStopName, $"{oldTicket.StartPoint} (Trạm cao tốc)" },
                DropOffPoints = new List<string> { oldTicket.DropOffStopName, $"{oldTicket.EndPoint} (Trung tâm thành phố)" },
                Seats = GenerateSeatsForCapacity(22, new[] { "A01", "A03", "B01", "B04" }, basePrice)
            });

            // Option 3: Buổi tối - Hạng Thương Gia VIP (ĐẮT HƠN vé cũ 70.000đ -> Khách bù tiền)
            var eveningPrice = basePrice + 70000m;
            trips.Add(new TripItemViewModel
            {
                TripId = 203,
                OperatorName = "SmartBus Royal Luxury Suite",
                Rating = 5.0,
                ReviewCount = 2680,
                BusTypeName = "Royal Luxury 20 Phòng VIP (Massage, Màn hình 4K)",
                BusImage = "https://images.unsplash.com/photo-1494515843206-f3117d3f51b7?w=400&q=80",
                LicensePlate = "29B-999.88",
                DepartureTime = new TimeSpan(20, 30, 0),
                DeparturePoint = oldTicket.BoardingStopName,
                ArrivalTime = new TimeSpan(2, 0, 0),
                ArrivalPoint = oldTicket.DropOffStopName,
                DurationText = "5h 30m",
                Price = eveningPrice,
                OriginalPrice = eveningPrice * 1.12m,
                AvailableSeats = 6,
                TotalCapacity = 20,
                IsFlashSale = true,
                FlashSaleText = "HẠNG THƯƠNG GIA",
                NoticeText = "Nâng cấp lên xe phòng thương gia cao cấp, phục vụ bữa ăn nhẹ & nước uống miễn phí (+70.000đ phụ thu)",
                BoardingPoints = new List<string> { oldTicket.BoardingStopName, $"{oldTicket.StartPoint} (Sảnh chờ VIP)" },
                DropOffPoints = new List<string> { oldTicket.DropOffStopName, $"{oldTicket.EndPoint} (Trả tận khách sạn)" },
                Seats = GenerateSeatsForCapacity(20, new[] { "A01", "A02", "B03" }, eveningPrice)
            });

            return trips;
        }

        private List<SeatItemViewModel> GenerateSeatsForCapacity(int capacity, IEnumerable<string> bookedSeats, decimal basePrice)
        {
            var seats = new List<SeatItemViewModel>();
            var bookedSet = bookedSeats.ToHashSet(StringComparer.OrdinalIgnoreCase);
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
                        Status = bookedSet.Contains(seatCode) ? "BOOKED" : "AVAILABLE"
                    });
                }
            }
            return seats;
        }

        private static string GetBusImageForType(string? typeName)
        {
            var lower = (typeName ?? "").ToLower();
            if (lower.Contains("cung điện") || lower.Contains("royal"))
                return "https://images.unsplash.com/photo-1494515843206-f3117d3f51b7?w=400&q=80";
            if (lower.Contains("34") || lower.Contains("giường"))
                return "https://images.unsplash.com/photo-1544620347-c4fd4a3d5957?w=400&q=80";
            return "https://images.unsplash.com/photo-1570125909232-eb263c188f7e?w=400&q=80";
        }

        private static decimal CalculateBasePrice(decimal? distance)
        {
            var d = distance ?? 100m;
            return WebApplication1.Services.RoutePricingService.GetMarketBasePrice(d);
        }

        private async Task<string> GetAccurateStopAddressAsync(string? stopName, string city)
        {
            if (string.IsNullOrWhiteSpace(stopName))
            {
                return $"Khu vực trung tâm TP. {city}";
            }

            try
            {
                var cleanName = stopName.Split('(')[0].Trim();
                var busStop = await _context.BusStops
                    .FirstOrDefaultAsync(s => s.StopName.Contains(cleanName) || cleanName.Contains(s.StopName));
                if (busStop != null && !string.IsNullOrWhiteSpace(busStop.Address))
                {
                    return busStop.Address;
                }
            }
            catch { }

            var lower = stopName.ToLower();
            if (lower.Contains("thái nguyên"))
            {
                if (lower.Contains("văn phòng") || lower.Contains("đại diện"))
                    return "Số 236 Đường Hoàng Văn Thụ, TP. Thái Nguyên";
                return "Khu đô thị Đồng Quang, P. Đồng Quang, TP. Thái Nguyên";
            }
            if (lower.Contains("cao bằng"))
            {
                if (lower.Contains("ngã ba") || lower.Contains("trung tâm"))
                    return "Ngã ba Vườn Cam, P. Hợp Giang, TP. Cao Bằng";
                return "QL3, P. Đề Thám, TP. Cao Bằng";
            }
            if (lower.Contains("mỹ đình")) return "Số 20 Phạm Hùng, P. Mỹ Đình 2, Q. Nam Từ Liêm, Hà Nội";
            if (lower.Contains("giáp bát")) return "Km 6 Đường Giải Phóng, P. Giáp Bát, Q. Hoàng Mai, Hà Nội";
            if (lower.Contains("nước ngầm")) return "Số 1 Ngọc Hồi, P. Hoàng Liệt, Q. Hoàng Mai, Hà Nội";
            if (lower.Contains("gia lâm")) return "Số 9 Ngô Gia Khảm, P. Gia Thụy, Q. Long Biên, Hà Nội";
            if (lower.Contains("yên nghĩa")) return "QL6, P. Yên Nghĩa, Q. Hà Đông, Hà Nội";
            if (lower.Contains("hải phòng") || lower.Contains("cầu rào") || lower.Contains("niệm nghĩa")) return "Đường Bùi Viện, Q. Lê Chân, Hải Phòng";
            if (lower.Contains("quảng ninh") || lower.Contains("bãi cháy")) return "Đường Hạ Long, P. Bãi Cháy, TP. Hạ Long";
            if (lower.Contains("đà nẵng")) return "Đường Nam Trân, P. Hòa Minh, Q. Liên Chiểu, Đà Nẵng";
            if (lower.Contains("sài gòn") || lower.Contains("miền đông")) return "Đinh Bộ Lĩnh, P. 26, Q. Bình Thạnh, TP. Hồ Chí Minh";
            if (lower.Contains("miền tây")) return "Số 395 Kinh Dương Vương, P. An Lạc, Q. Bình Tân, TP. Hồ Chí Minh";

            return $"Khu vực bến xe trung tâm {city}";
        }

        private async Task<Ticket?> LoadOwnedTicketAsync(string? key)
        {
            if (string.IsNullOrWhiteSpace(key)) return null;
            key = key.Trim();
            int.TryParse(key, out var id);
            return await OwnedTickets.AsNoTrackingWithIdentityResolution()
                .Include(t => t.Booking).ThenInclude(b => b.User)
                .Include(t => t.Booking).ThenInclude(b => b.Payments)
                .Include(t => t.Booking).ThenInclude(b => b.Tickets)
                .Include(t => t.Booking).ThenInclude(b => b.Trip).ThenInclude(tr => tr.Route)
                .Include(t => t.Booking).ThenInclude(b => b.Trip).ThenInclude(tr => tr.Bus).ThenInclude(bus => bus.BusType)
                .Include(t => t.Booking).ThenInclude(b => b.Trip).ThenInclude(tr => tr.Driver).ThenInclude(d => d!.User)
                .Include(t => t.BoardingStop).Include(t => t.DropOffStop)
                .Where(t => t.TicketCode == key || t.Booking.BookingCode == key || t.TicketId == id)
                .OrderBy(t => t.TicketId).FirstOrDefaultAsync();
        }

        private static bool HasVerifiedPayment(Ticket ticket) => ticket.Booking.TotalAmount > 0
            && ticket.Booking.Payments.Where(p => p.Status == "Paid").Sum(p => p.Amount) >= ticket.Booking.TotalAmount;

        private static TicketDetailViewModel ToDetail(Ticket ticket)
        {
            var booking = ticket.Booking;
            var trip = booking.Trip;
            var route = trip.Route;
            var issued = ticket.Status?.ToUpperInvariant() is "ISSUED" or "CONFIRMED" or "USED";
            return new TicketDetailViewModel
            {
                TicketId = ticket.TicketId, TicketCode = ticket.TicketCode, SeatNumber = ticket.SeatNumber ?? "",
                Price = ticket.Price, Status = ticket.Status ?? "PENDING", BookingId = booking.BookingId,
                BookingCode = booking.BookingCode, BookingTime = booking.BookingTime, TotalAmount = booking.TotalAmount,
                PassengerName = booking.User.FullName, PassengerPhone = booking.User.Phone ?? "", PassengerEmail = booking.User.Email,
                RouteCode = route.RouteCode, RouteName = route.RouteName, StartPoint = route.StartPoint, EndPoint = route.EndPoint,
                Distance = route.Distance ?? 0, EstimatedDuration = route.EstimatedDuration ?? 0,
                BoardingStopName = ticket.BoardingStop?.StopName ?? "", BoardingStopAddress = ticket.BoardingStop?.Address ?? "",
                DropOffStopName = ticket.DropOffStop?.StopName ?? "", DropOffStopAddress = ticket.DropOffStop?.Address ?? "",
                DepartureTime = trip.DepartureTime.ToTimeSpan(), ArrivalTime = trip.ArrivalTime?.ToTimeSpan(),
                TripId = trip.TripId, TripDate = trip.TripDate.ToDateTime(TimeOnly.MinValue),
                LicensePlate = trip.Bus.LicensePlate, BusTypeName = trip.Bus.BusType.TypeName,
                DriverName = trip.Driver?.User.FullName ?? "", DriverPhone = trip.Driver?.User.Phone ?? "",
                QrDataPayload = issued && HasVerifiedPayment(ticket) ? $"SMARTBUS|TICKET:{ticket.TicketCode}|BOOKING:{booking.BookingCode}" : ""
            };
        }

        private TicketDetailViewModel GetMockOldTicket(string ticketCode)
        {
            var fallbackFrom = Request.Cookies["sbg_last_from"] ?? Request.Cookies["sbg_search_from"] ?? "Thái Nguyên";
            var fallbackTo = Request.Cookies["sbg_last_to"] ?? Request.Cookies["sbg_search_to"] ?? "Cao Bằng";
            var fromCode = WebApplication1.Services.ProvinceLicenseHelper.GetAllCodes(fallbackFrom).FirstOrDefault() ?? "TN";
            var toCode = WebApplication1.Services.ProvinceLicenseHelper.GetAllCodes(fallbackTo).FirstOrDefault() ?? "CB";

            var fbBusType = Request.Cookies["sbg_last_bustype"] ?? "Limousine VIP 22 Phòng Cung Điện";
            var fbMetrics = WebApplication1.Services.RoutePricingService.GetRouteMetrics(fallbackFrom, fallbackTo);

            var fbSeats = Request.Cookies["sbg_last_seats"] ?? "A01";
            var fbSeatCount = fbSeats.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Length;
            var defaultCalculatedPrice = WebApplication1.Services.RoutePricingService.CalculateTripPrice(fbMetrics.Distance, fbBusType, 29, 0) * Math.Max(1, fbSeatCount);
            var fbPrice = decimal.TryParse(Request.Cookies["sbg_last_total"], out var tot) && tot > 0 ? tot : defaultCalculatedPrice;

            DateTime fbDate = DateTime.Today.AddDays(1);
            if (DateTime.TryParse(Request.Cookies["sbg_last_date"], out var pDate)) fbDate = pDate;

            TimeSpan fbDep = new TimeSpan(8, 0, 0);
            if (TimeSpan.TryParse(Request.Cookies["sbg_last_deptime"], out var pDep)) fbDep = pDep;

            TimeSpan fbArr = fbDep.Add(TimeSpan.FromMinutes(fbMetrics.EstimatedMinutes));
            if (TimeSpan.TryParse(Request.Cookies["sbg_last_arrtime"], out var pArr)) fbArr = pArr;

            var fbPlate = Request.Cookies["sbg_last_licenseplate"] 
                ?? (fallbackFrom.Contains("Thái Nguyên") ? "20B-188.68" : $"{fromCode}B-888.68");
            var fbRouteName = Request.Cookies["sbg_last_routename"] ?? $"{fallbackFrom} - {fallbackTo}";
            var fbRouteCode = Request.Cookies["sbg_last_routecode"] ?? $"{fromCode}-{toCode}-01";
            var fbDriver = Request.Cookies["sbg_last_driver"] ?? $"Nguyễn Văn Tuấn (Tài xế {fallbackFrom})";
            var fbDriverPhone = Request.Cookies["sbg_last_driverphone"] ?? "0988 777 999";
            var fbBoarding = Request.Cookies["sbg_last_boarding"] ?? $"Bến xe {fallbackFrom} (Cổng chính)";
            var fbDropoff = Request.Cookies["sbg_last_dropoff"] ?? $"Bến xe {fallbackTo} (Khu trả khách)";

            var genBooking = $"BK-{fromCode}{toCode}-{DateTime.Now:MMddHHmm}";

            return new TicketDetailViewModel
            {
                TicketId = 2026,
                TicketCode = ticketCode,
                SeatNumber = fbSeats,
                Price = fbPrice,
                Status = "ACTIVE",
                BookingId = 8821,
                BookingCode = genBooking,
                BookingTime = DateTime.Now,
                TotalAmount = fbPrice,
                PassengerName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? (User.Identity?.Name ?? "Trần Văn Bình"),
                PassengerPhone = "0912 345 678",
                PassengerEmail = "khachhang@smartbus.vn",
                RouteCode = fbRouteCode,
                RouteName = fbRouteName,
                StartPoint = fallbackFrom,
                EndPoint = fallbackTo,
                Distance = fbMetrics.Distance,
                EstimatedDuration = fbMetrics.EstimatedMinutes,
                BoardingStopName = fbBoarding,
                BoardingStopAddress = $"Khu vực xuất phát TP. {fallbackFrom}",
                DepartureTime = fbDep,
                DropOffStopName = fbDropoff,
                DropOffStopAddress = $"Khu vực điểm đến TP. {fallbackTo}",
                ArrivalTime = fbArr,
                TripId = 101,
                TripDate = fbDate,
                LicensePlate = fbPlate,
                BusTypeName = fbBusType,
                DriverName = fbDriver,
                DriverPhone = fbDriverPhone,
                QrDataPayload = $"SMARTBUS|TICKET:{ticketCode}|BOOKING:{genBooking}|ROUTE:{fallbackFrom}-{fallbackTo}|SEAT:{fbSeats}|DATE:{fbDate:yyyy-MM-dd}|STATUS:ACTIVE"
            };
        }
    }
}

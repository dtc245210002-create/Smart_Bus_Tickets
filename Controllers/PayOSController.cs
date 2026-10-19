using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models.Entities;

namespace WebApplication1.Controllers
{
    public class PayOSController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PayOSController> _logger;

        public PayOSController(ApplicationDbContext context, ILogger<PayOSController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // =========================================================================
        // GET: /payos/mobile-pay
        // Giao diện thanh toán giả lập payOS Sandbox tối ưu cho màn hình ĐIỆN THOẠI
        // (Mở ra khi dùng Camera điện thoại hoặc Zalo quét mã QR trên máy tính)
        // =========================================================================
        [HttpGet]
        [Route("payos/mobile-pay")]
        public async Task<IActionResult> MobilePay(
            string? ticketCode = null,
            string? bookingCode = null,
            decimal? amount = null,
            string? from = null,
            string? to = null,
            string? seats = null,
            string? depTime = null,
            string? busType = null,
            string? passenger = null)
        {
            var effTicketCode = !string.IsNullOrEmpty(ticketCode) ? ticketCode : "SBG-TN-CB-001";
            var effBookingCode = !string.IsNullOrEmpty(bookingCode) ? bookingCode : "BK-TNCB-001";
            var effAmount = amount ?? 180000m;
            var effFrom = from ?? "Thái Nguyên";
            var effTo = to ?? "Cao Bằng";
            var effSeats = seats ?? "A01";
            var effDepTime = depTime ?? "08:00";
            var effBusType = busType ?? "Xe Limousine VIP";
            var effPassenger = passenger ?? (User.Identity?.Name ?? "Lê Văn Cường");

            // Thử lấy thông tin chính xác từ Database nếu có
            try
            {
                var ticket = await _context.Tickets
                    .Include(t => t.Booking).ThenInclude(b => b.User)
                    .Include(t => t.Booking).ThenInclude(b => b.Trip).ThenInclude(tr => tr.Route)
                    .Include(t => t.Booking).ThenInclude(b => b.Trip).ThenInclude(tr => tr.Bus).ThenInclude(bus => bus.BusType)
                    .FirstOrDefaultAsync(t => t.TicketCode == effTicketCode ||
                                              (!string.IsNullOrEmpty(bookingCode) && t.Booking.BookingCode == bookingCode));

                if (ticket != null)
                {
                    effTicketCode = ticket.TicketCode;
                    effBookingCode = ticket.Booking?.BookingCode ?? effBookingCode;
                    effAmount = ticket.Booking?.TotalAmount ?? ticket.Price;
                    effSeats = ticket.SeatNumber ?? effSeats;
                    effPassenger = ticket.Booking?.User?.FullName ?? effPassenger;
                    if (ticket.Booking?.Trip?.Route != null)
                    {
                        effFrom = ticket.Booking.Trip.Route.StartPoint;
                        effTo = ticket.Booking.Trip.Route.EndPoint;
                    }
                    if (ticket.Booking?.Trip?.Bus?.BusType != null)
                    {
                        effBusType = ticket.Booking.Trip.Bus.BusType.TypeName;
                    }
                    if (ticket.Booking?.Trip != null)
                    {
                        effDepTime = ticket.Booking.Trip.DepartureTime.ToString(@"hh\:mm");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Không thể tải thông tin vé từ DB cho MobilePay: {Message}", ex.Message);
            }

            ViewBag.TicketCode = effTicketCode;
            ViewBag.BookingCode = effBookingCode;
            ViewBag.Amount = effAmount;
            ViewBag.From = effFrom;
            ViewBag.To = effTo;
            ViewBag.Seats = effSeats;
            ViewBag.DepTime = effDepTime;
            ViewBag.BusType = effBusType;
            ViewBag.Passenger = effPassenger;
            ViewBag.OrderCode = Math.Abs(effTicketCode.GetHashCode() % 100000000);

            return View();
        }

        // =========================================================================
        // POST: /api/payos/simulate-payment
        // API xác nhận thanh toán payOS Sandbox từ điện thoại di động hoặc máy tính
        // =========================================================================
        [HttpPost]
        [Route("api/payos/simulate-payment")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> SimulatePayment([FromBody] SimulatePaymentDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.TicketCode))
            {
                return BadRequest(new { success = false, message = "Dữ liệu vé không hợp lệ." });
            }

            try
            {
                var cleanCode = dto.TicketCode.Trim();
                var cleanBooking = dto.BookingCode?.Trim();
                var noDashCode = cleanCode.Replace("-", "");
                var noDashBooking = !string.IsNullOrEmpty(cleanBooking) ? cleanBooking.Replace("-", "") : "";

                var ticket = await _context.Tickets
                    .Include(t => t.Booking).ThenInclude(b => b.Tickets)
                    .Include(t => t.Booking).ThenInclude(b => b.Payments)
                    .FirstOrDefaultAsync(t => t.TicketCode == cleanCode ||
                                              t.TicketCode.Replace("-", "") == noDashCode ||
                                              t.TicketCode.StartsWith(cleanCode + "-") ||
                                              (!string.IsNullOrEmpty(cleanBooking) && t.Booking.BookingCode == cleanBooking) ||
                                              (!string.IsNullOrEmpty(noDashBooking) && t.Booking.BookingCode.Replace("-", "") == noDashBooking));

                var txnCode = $"PAYOS{DateTime.Now:yyyyMMddHHmmss}{Random.Shared.Next(100, 999)}";

                if (ticket != null)
                {
                    ticket.Status = "CONFIRMED";
                    if (ticket.Booking != null)
                    {
                        ticket.Booking.Status = "Confirmed";
                        foreach (var subTicket in ticket.Booking.Tickets)
                        {
                            subTicket.Status = "CONFIRMED";
                        }

                        // Lưu bản ghi Payment
                        var payment = new Payment
                        {
                            BookingId = ticket.Booking.BookingId,
                            Amount = dto.Amount > 0 ? dto.Amount : ticket.Booking.TotalAmount,
                            PaymentMethod = "payOS (Sandbox)",
                            Status = "Paid",
                            PaymentTime = DateTime.Now,
                            TransactionCode = txnCode
                        };
                        _context.Payments.Add(payment);
                    }

                    await _context.SaveChangesAsync();
                }

                // Ghi nhận vào DemoPaymentStore để API polling check-status nhận diện thành công ngay lập tức
                WebApplication1.Services.DemoPaymentStore.MarkPaid(cleanCode, cleanBooking, dto.Amount, txnCode, "payOS (Sandbox Demo)");

                _logger.LogInformation("Thanh toán payOS Sandbox thành công cho mã vé [{TicketCode}] - Txn: [{Txn}]", cleanCode, txnCode);

                return Ok(new
                {
                    success = true,
                    status = "SUCCESS",
                    isPaid = true,
                    transactionCode = txnCode,
                    ticketCode = cleanCode,
                    bookingCode = cleanBooking,
                    amount = dto.Amount,
                    message = "Thanh toán qua payOS Sandbox thành công (Không bị trừ tiền thật)!"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý mô phỏng thanh toán payOS: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = "Lỗi máy chủ khi cập nhật thanh toán." });
            }
        }
    }

    public class SimulatePaymentDto
    {
        public string? TicketCode { get; set; }
        public string? BookingCode { get; set; }
        public decimal Amount { get; set; }
        public string? BankCode { get; set; }
        public long? OrderCode { get; set; }
    }
}

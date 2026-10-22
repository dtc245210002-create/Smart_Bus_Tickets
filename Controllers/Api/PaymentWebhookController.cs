using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models.Entities;
using WebApplication1.Models.Settings;
using WebApplication1.Services;
using WebApplication1.Services.Email;
using WebApplication1.Services.Payment;

namespace WebApplication1.Controllers.Api
{
    [ApiController]
    [Route("api/payment")]
    public class PaymentWebhookController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IVietQrService _vietQrService;
        private readonly ISeatHoldService _seatHoldService;
        private readonly IEmailService _emailService;
        private readonly ILogger<PaymentWebhookController> _logger;

        public PaymentWebhookController(
            ApplicationDbContext context,
            IVietQrService vietQrService,
            ISeatHoldService seatHoldService,
            IEmailService emailService,
            ILogger<PaymentWebhookController> logger)
        {
            _context = context;
            _vietQrService = vietQrService;
            _seatHoldService = seatHoldService;
            _emailService = emailService;
            _logger = logger;
        }

        // =========================================================================
        // 1. LẤY & CẬP NHẬT CẤU HÌNH TÀI KHOẢN NGÂN HÀNG
        // =========================================================================

        [HttpGet("bank-settings")]
        public IActionResult GetBankSettings()
        {
            var settings = _vietQrService.GetBankSettings();
            var banks = _vietQrService.GetSupportedBanks();

            return Ok(new
            {
                success = true,
                settings = settings,
                supportedBanks = banks
            });
        }

        [HttpPost("bank-settings")]
        public IActionResult UpdateBankSettings([FromBody] BankSettings newSettings)
        {
            if (newSettings == null || string.IsNullOrWhiteSpace(newSettings.AccountNo) || string.IsNullOrWhiteSpace(newSettings.AccountName))
            {
                return BadRequest(new { success = false, message = "Số tài khoản và Tên chủ tài khoản không được để trống." });
            }

            // Tự động tìm tên ngân hàng tương ứng theo mã
            var banks = _vietQrService.GetSupportedBanks();
            var matched = banks.FirstOrDefault(b => b.Code.Equals(newSettings.BankId, StringComparison.OrdinalIgnoreCase));
            if (matched != null)
            {
                newSettings.BankName = matched.Name + " (" + matched.ShortName + ")";
            }

            newSettings.AccountName = newSettings.AccountName.Trim().ToUpperInvariant();
            newSettings.AccountNo = newSettings.AccountNo.Trim();

            _vietQrService.UpdateBankSettings(newSettings);

            return Ok(new
            {
                success = true,
                message = $"Đã cập nhật thành công tài khoản nhận tiền: {newSettings.BankName} - STK: {newSettings.AccountNo} - Tên: {newSettings.AccountName}",
                settings = _vietQrService.GetBankSettings()
            });
        }

        // =========================================================================
        // 2. WEBHOOK NHẬN TIỀN TỰ ĐỘNG TỪ SEPAY (sepay.vn) & NGÂN HÀNG THẬT
        // URL cấu hình trên SePAY: https://<domain-cua-ban>/api/payment/sepay-webhook
        // =========================================================================

        [HttpPost("sepay-webhook")]
        public async Task<IActionResult> SePayWebhook([FromBody] SePayWebhookPayload payload)
        {
            if (payload == null)
            {
                return BadRequest(new { success = false, message = "Payload rỗng." });
            }

            _logger.LogInformation("Nhận Webhook chuyển khoản ngân hàng: Số tiền [{Amount}] - Nội dung [{Content}] - Cổng [{Gateway}] - GD [{RefCode}]",
                payload.TransferAmount, payload.Content, payload.Gateway, payload.ReferenceCode);

            // Chỉ xử lý giao dịch nhận tiền (transferType = "in")
            if (!string.Equals(payload.TransferType, "in", StringComparison.OrdinalIgnoreCase) && payload.TransferAmount <= 0)
            {
                return Ok(new { success = true, message = "Bỏ qua giao dịch chi tiền." });
            }

            var content = payload.Content ?? string.Empty;
            var amount = payload.TransferAmount;

            var result = await ProcessRealBankTransferAsync(content, amount, payload.ReferenceCode ?? $"SEPAY_{payload.Id}", payload.Gateway ?? "VietQR");
            if (result.Success)
            {
                return Ok(new { success = true, message = result.Message, ticketCode = result.TicketCode });
            }

            return Ok(new { success = false, message = result.Message });
        }

        // =========================================================================
        // 3. API GIẢ LẬP / DEMO BẮN GIAO DỊCH CHUYỂN TIỀN THẬT (Dành cho test nhanh)
        // =========================================================================

        [HttpPost("simulate-transfer")]
        public async Task<IActionResult> SimulateTransfer([FromBody] SimulateTransferRequest request)
        {
            var content = request?.Content ?? request?.TicketCode ?? string.Empty;
            var amount = request?.Amount ?? 180000m;
            var txnCode = $"SIM_{DateTime.Now:yyyyMMddHHmmss}";

            var result = await ProcessRealBankTransferAsync(content, amount, txnCode, "VietQR Demo");
            return Ok(result);
        }

        // =========================================================================
        // HÀM XỬ LÝ LƯU GIAO DỊCH & KÍCH HOẠT VÉ ĐIỆN TỬ
        // =========================================================================

        private async Task<TransferProcessResult> ProcessRealBankTransferAsync(string rawContent, decimal transferAmount, string txnCode, string gateway)
        {
            if (string.IsNullOrWhiteSpace(rawContent))
            {
                return new TransferProcessResult { Success = false, Message = "Nội dung chuyển khoản trống." };
            }

            // Trích xuất mã vé dạng SBG-XXXXXX hoặc SBGXXXXXX
            var clean = rawContent.ToUpperInvariant();
            
            // Tìm kiếm vé trong CSDL
            Ticket? matchedTicket = null;

            // 1. Thử khớp chính xác
            matchedTicket = await _context.Tickets
                .Include(t => t.Booking).ThenInclude(b => b.User)
                .Include(t => t.Booking).ThenInclude(b => b.Trip).ThenInclude(tr => tr.Route)
                .Include(t => t.Booking).ThenInclude(b => b.Trip).ThenInclude(tr => tr.Bus).ThenInclude(bus => bus.BusType)
                .Include(t => t.BoardingStop)
                .Include(t => t.DropOffStop)
                .FirstOrDefaultAsync(t => 
                    clean.Contains(t.TicketCode.Replace("-", "")) || 
                    clean.Contains(t.TicketCode) ||
                    (t.Booking != null && clean.Contains(t.Booking.BookingCode.Replace("-", ""))));

            // 2. Nếu không tìm thấy, quét mã regex SBG[0-9]+
            if (matchedTicket == null)
            {
                var match = Regex.Match(clean, @"SBG\s*([0-9A-Z\-]+)");
                if (match.Success)
                {
                    var extractedCode = match.Value.Replace(" ", "");
                    matchedTicket = await _context.Tickets
                        .Include(t => t.Booking).ThenInclude(b => b.User)
                        .Include(t => t.Booking).ThenInclude(b => b.Trip).ThenInclude(tr => tr.Route)
                        .Include(t => t.Booking).ThenInclude(b => b.Trip).ThenInclude(tr => tr.Bus).ThenInclude(bus => bus.BusType)
                        .Include(t => t.BoardingStop)
                        .Include(t => t.DropOffStop)
                        .FirstOrDefaultAsync(t => 
                            t.TicketCode.Replace("-", "").Contains(extractedCode) || 
                            extractedCode.Contains(t.TicketCode.Replace("-", "")));
                }
            }

            if (matchedTicket == null)
            {
                // Nếu là vé mới sinh chưa lưu hoặc đang ở phiên hiện tại, lấy vé gần nhất chưa thanh toán
                matchedTicket = await _context.Tickets
                    .Include(t => t.Booking).ThenInclude(b => b.User)
                    .Include(t => t.Booking).ThenInclude(b => b.Trip).ThenInclude(tr => tr.Route)
                    .Include(t => t.Booking).ThenInclude(b => b.Trip).ThenInclude(tr => tr.Bus).ThenInclude(bus => bus.BusType)
                    .OrderByDescending(t => t.TicketId)
                    .FirstOrDefaultAsync(t => t.Status == "PENDING" || t.Status == "HELD" || t.Status == "UNPAID");
            }

            if (matchedTicket == null)
            {
                return new TransferProcessResult
                {
                    Success = false,
                    Message = $"Không tìm thấy vé tương ứng với nội dung chuyển khoản: {rawContent}"
                };
            }

            // Cập nhật trạng thái vé sang CONFIRMED (Đã thanh toán thành công)
            matchedTicket.Status = "CONFIRMED";
            if (matchedTicket.Booking != null)
            {
                matchedTicket.Booking.Status = "Confirmed";

                // Lưu bản ghi thanh toán vào bảng Payments
                var payment = new Payment
                {
                    BookingId = matchedTicket.Booking.BookingId,
                    Amount = transferAmount > 0 ? transferAmount : (matchedTicket.Booking.TotalAmount > 0 ? matchedTicket.Booking.TotalAmount : matchedTicket.Price),
                    PaymentMethod = gateway,
                    TransactionCode = txnCode,
                    PaymentTime = DateTime.Now,
                    Status = "Paid"
                };
                _context.Payments.Add(payment);

                // Giải phóng giữ chỗ tạm thời
                if (matchedTicket.Booking.TripId > 0 && !string.IsNullOrEmpty(matchedTicket.SeatNumber))
                {
                    var seats = matchedTicket.SeatNumber.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    await _seatHoldService.ReleaseSeatsAsync(matchedTicket.Booking.TripId, seats, "payment_confirmed", null);
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Xác nhận thanh toán thành công cho vé [{TicketCode}] - Số tiền: {Amount:N0} đ", matchedTicket.TicketCode, transferAmount);

            // Gửi email vé điện tử tự động cho hành khách
            try
            {
                var recipientEmail = matchedTicket.Booking?.User?.Email;
                if (!string.IsNullOrEmpty(recipientEmail) && recipientEmail.Contains("@"))
                {
                    var trip = matchedTicket.Booking?.Trip;
                    var emailBody = $@"
                        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; border: 1px solid #10b981; border-radius: 12px; padding: 20px; background-color: #ffffff;'>
                            <h2 style='color: #0A4D46; text-align: center;'>SMARTBUS GO - VÉ ĐIỆN TỬ ĐÃ KÍCH HOẠT</h2>
                            <p>Xin chào <strong>{matchedTicket.Booking?.User?.FullName ?? "Quý khách"}</strong>,</p>
                            <p>Hệ thống đã nhận được số tiền thanh toán <strong>{transferAmount:N0} VNĐ</strong> qua chuyển khoản ngân hàng VietQR thành công!</p>
                            <div style='background-color: #f0fdf4; border: 1px solid #bbf7d0; padding: 15px; border-radius: 8px;'>
                                <p style='margin: 4px 0;'><strong>Mã vé điện tử:</strong> <span style='font-size: 18px; color: #0A4D46; font-weight: bold;'>{matchedTicket.TicketCode}</span></p>
                                <p style='margin: 4px 0;'><strong>Tuyến đường:</strong> {trip?.Route?.RouteName ?? "Tuyến liên tỉnh SmartBus"}</p>
                                <p style='margin: 4px 0;'><strong>Số ghế:</strong> <span style='color: #2563eb; font-weight: bold;'>{matchedTicket.SeatNumber}</span></p>
                                <p style='margin: 4px 0;'><strong>Ngày khởi hành:</strong> {trip?.TripDate:dd/MM/yyyy}</p>
                                <p style='margin: 4px 0;'><strong>Giờ xuất bến:</strong> {trip?.DepartureTime:hh\\:mm}</p>
                                <p style='margin: 4px 0;'><strong>Biển số xe:</strong> {trip?.Bus?.LicensePlate ?? "29B-888.88"}</p>
                            </div>
                            <p style='margin-top: 20px; font-size: 13px; color: #64748b;'>Quý khách vui lòng lưu lại mã vé này hoặc mở màn hình Vé điện tử để quét mã QR khi lên xe. Chúc quý khách một chuyến đi an toàn và thuận lợi!</p>
                        </div>";

                    await _emailService.SendEmailAsync(recipientEmail, $"[SmartBus Go] Xác nhận vé xe #{matchedTicket.TicketCode} thành công", emailBody);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Không thể gửi email xác nhận vé: {Message}", ex.Message);
            }

            return new TransferProcessResult
            {
                Success = true,
                Message = $"Đã xác nhận thanh toán thành công #{txnCode} cho vé {matchedTicket.TicketCode}!",
                TicketCode = matchedTicket.TicketCode,
                BookingCode = matchedTicket.Booking?.BookingCode,
                Amount = transferAmount
            };
        }
    }

    public class SePayWebhookPayload
    {
        public long Id { get; set; }
        public string? Gateway { get; set; }
        public string? TransactionDate { get; set; }
        public string? AccountNumber { get; set; }
        public string? Content { get; set; }
        public string? TransferType { get; set; }
        public decimal TransferAmount { get; set; }
        public string? ReferenceCode { get; set; }
        public string? Description { get; set; }
    }

    public class SimulateTransferRequest
    {
        public string? TicketCode { get; set; }
        public string? Content { get; set; }
        public decimal Amount { get; set; } = 180000m;
    }

    public class TransferProcessResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? TicketCode { get; set; }
        public string? BookingCode { get; set; }
        public decimal Amount { get; set; }
    }
}

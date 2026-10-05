using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Services.BackgroundQueue;
using WebApplication1.Services.Notifications;
using WebApplication1.Services.Notifications.Models;
using WebApplication1.Services.Pdf;
using WebApplication1.Services.Qr;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/ticket-notification")]
    public class TicketNotificationApiController : ControllerBase
    {
        private readonly IQrCodeService _qrCodeService;
        private readonly ITicketPdfService _pdfService;
        private readonly INotificationTemplateService _templateService;
        private readonly IBackgroundTaskQueue _taskQueue;

        public TicketNotificationApiController(
            IQrCodeService qrCodeService,
            ITicketPdfService pdfService,
            INotificationTemplateService templateService,
            IBackgroundTaskQueue taskQueue)
        {
            _qrCodeService = qrCodeService;
            _pdfService = pdfService;
            _templateService = templateService;
            _taskQueue = taskQueue;
        }

        /// <summary>
        /// 1. API sinh ảnh QR Code trực tiếp (Content-Type: image/png)
        /// </summary>
        [HttpGet("generate-qr")]
        public IActionResult GenerateQr(string? ticketCode = "SBG-HN-HP-20261024-001")
        {
            var securePayload = _qrCodeService.BuildSecureTicketPayload(
                ticketCode ?? "SBG-SAMPLE",
                "BK-998822",
                "VIP-08",
                "R01",
                DateTime.Today.ToString("dd/MM/yyyy"),
                "19:30",
                450000m);

            var pngBytes = _qrCodeService.GenerateQrPngBytes(securePayload.ToPayloadString(), pixelsPerModule: 15);
            return File(pngBytes, "image/png", $"QR_{ticketCode}.png");
        }

        /// <summary>
        /// 2. API sinh mã QR dạng SVG vector nét căng
        /// </summary>
        [HttpGet("generate-qr-svg")]
        public IActionResult GenerateQrSvg(string? ticketCode = "SBG-HN-HP-20261024-001")
        {
            var securePayload = _qrCodeService.BuildSecureTicketPayload(
                ticketCode ?? "SBG-SAMPLE",
                "BK-998822",
                "VIP-08",
                "R01",
                DateTime.Today.ToString("dd/MM/yyyy"),
                "19:30",
                450000m);

            var svgContent = _qrCodeService.GenerateQrSvg(securePayload.ToPayloadString());
            return Content(svgContent, "image/svg+xml");
        }

        /// <summary>
        /// 3. API kiểm tra tính hợp lệ và chữ ký HMAC của QR payload
        /// </summary>
        [HttpGet("validate-qr")]
        public IActionResult ValidateQr(string payload)
        {
            var isValid = _qrCodeService.ValidateTicketPayload(payload, out var reason);
            return Ok(new
            {
                Valid = isValid,
                Message = reason,
                Payload = payload
            });
        }

        /// <summary>
        /// 4. API xem trước Template Email HTML trên trình duyệt
        /// </summary>
        [HttpGet("preview-email")]
        public IActionResult PreviewEmail(string? ticketCode = "SBG-HN-DN-20261024-008", string? email = "khachhang@gmail.com")
        {
            var model = CreateSampleTicket(ticketCode, email);
            var securePayload = _qrCodeService.BuildSecureTicketPayload(
                model.TicketCode,
                model.BookingCode,
                model.SeatNumber,
                model.RouteName,
                model.DepartureDate,
                model.DepartureTime,
                model.Price);

            model.QrPayload = securePayload.ToPayloadString();
            model.QrCodeBase64 = _qrCodeService.GenerateQrBase64(model.QrPayload);

            var html = _templateService.RenderTicketEmailHtml(model);
            return Content(html, "text/html; charset=utf-8");
        }

        /// <summary>
        /// 5. API xem trước Template Tin nhắn SMS & Zalo ZNS
        /// </summary>
        [HttpGet("preview-sms-zalo")]
        public IActionResult PreviewSmsZalo(string? ticketCode = "SBG-HN-DN-20261024-008")
        {
            var model = CreateSampleTicket(ticketCode, "0912345678@gmail.com");
            var smsText = _templateService.RenderTicketSmsText(model);
            var zaloJson = _templateService.RenderTicketZaloZnsJson(model);

            return Ok(new
            {
                TicketCode = model.TicketCode,
                Sms = new
                {
                    Content = smsText,
                    Length = smsText.Length,
                    Standard = "SMS Brandname <= 160 ký tự"
                },
                ZaloZns = System.Text.Json.JsonDocument.Parse(zaloJson).RootElement
            });
        }

        /// <summary>
        /// 6. API tải về file PDF vé điện tử có đính kèm mã QR
        /// </summary>
        [HttpGet("generate-pdf")]
        public async Task<IActionResult> GeneratePdf(string? ticketCode = "SBG-HN-DN-20261024-008")
        {
            var model = CreateSampleTicket(ticketCode, "khachhang@gmail.com");
            var securePayload = _qrCodeService.BuildSecureTicketPayload(
                model.TicketCode,
                model.BookingCode,
                model.SeatNumber,
                model.RouteName,
                model.DepartureDate,
                model.DepartureTime,
                model.Price);

            var qrBytes = _qrCodeService.GenerateQrPngBytes(securePayload.ToPayloadString(), pixelsPerModule: 15);
            var pdfBytes = await _pdfService.GenerateTicketPdfAsync(model, qrBytes);

            return File(pdfBytes, "application/pdf", $"Ve_Xe_SmartBus_{model.TicketCode}.pdf");
        }

        /// <summary>
        /// 7. API đẩy tác vụ gửi vé vào Hàng đợi ngầm (Background Worker)
        /// Phản hồi ngay 202 Accepted, worker sẽ tự xử lý gửi email ngầm
        /// </summary>
        [HttpPost("queue-ticket-email")]
        public async Task<IActionResult> QueueTicketEmail([FromBody] TicketNotificationModel? request)
        {
            var ticketData = request ?? CreateSampleTicket(null, "customer@example.com");

            await _taskQueue.QueueTicketEmailAsync(new TicketEmailTask
            {
                TicketData = ticketData,
                AttachPdf = true,
                AttachQrImage = true
            });

            return Accepted(new
            {
                Success = true,
                Message = $"Tác vụ gửi vé #{ticketData.TicketCode} đã được xếp vào hàng đợi ngầm xử lý.",
                QueuedAt = DateTime.UtcNow,
                Recipient = ticketData.PassengerEmail
            });
        }

        private static TicketNotificationModel CreateSampleTicket(string? ticketCode, string? email)
        {
            return new TicketNotificationModel
            {
                TicketCode = ticketCode ?? "SBG-HN-DN-20261024-008",
                BookingCode = "BK-988214",
                PassengerName = "Nguyễn Văn An",
                PassengerEmail = email ?? "nguyenvanan@gmail.com",
                PassengerPhone = "0912 345 678",
                RouteName = "Hà Nội - Đà Nẵng (Cao tốc Bắc Nam)",
                StartPoint = "Bến xe Nước Ngầm, Hà Nội",
                EndPoint = "Bến xe Trung Tâm Đà Nẵng",
                DepartureDate = DateTime.Today.AddDays(1).ToString("dd/MM/yyyy"),
                DepartureTime = "19:30",
                SeatNumber = "VIP-05",
                BusTypeName = "Limousine VIP 22 Phòng Đơn Cung Điện",
                LicensePlate = "29B-888.68",
                BoardingStopName = "Bến xe Nước Ngầm (Cổng A2)",
                BoardingStopAddress = "Km 8 Giải Phóng, P. Hoàng Liệt, Q. Hoàng Mai, Hà Nội",
                DropOffStopName = "Bến xe Trung Tâm Đà Nẵng (Cột 04)",
                DropOffStopAddress = "Đường Nam Trân, P. Hòa Minh, Q. Liên Chiểu, Đà Nẵng",
                Price = 450000m,
                WebTicketUrl = $"https://smartbus.vn/Ticket/Detail/{(ticketCode ?? "SBG-HN-DN-20261024-008")}"
            };
        }
    }
}

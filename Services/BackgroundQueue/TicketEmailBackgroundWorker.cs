using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebApplication1.Services.Notifications;
using WebApplication1.Services.Pdf;
using WebApplication1.Services.Qr;

namespace WebApplication1.Services.BackgroundQueue
{
    /// <summary>
    /// Background Service chạy ngầm, liên tục lắng nghe hàng đợi để xử lý sinh QR, PDF và gửi Email
    /// Giúp API / Web không bị lag hoặc block khi người dùng thanh toán đặt vé
    /// </summary>
    public class TicketEmailBackgroundWorker : BackgroundService
    {
        private readonly IBackgroundTaskQueue _taskQueue;
        private readonly IQrCodeService _qrCodeService;
        private readonly INotificationTemplateService _templateService;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TicketEmailBackgroundWorker> _logger;

        public TicketEmailBackgroundWorker(
            IBackgroundTaskQueue taskQueue,
            IQrCodeService qrCodeService,
            INotificationTemplateService templateService,
            IServiceScopeFactory scopeFactory,
            ILogger<TicketEmailBackgroundWorker> logger)
        {
            _taskQueue = taskQueue;
            _qrCodeService = qrCodeService;
            _templateService = templateService;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("=== SmartBus Ticket Email Worker đã bắt đầu chạy ngầm ===");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Lấy tác vụ từ hàng đợi Channel
                    var task = await _taskQueue.DequeueAsync(stoppingToken);
                    var ticket = task.TicketData;

                    _logger.LogInformation("Đang xử lý gửi email cho vé #{TicketCode} tới {Email}...", ticket.TicketCode, ticket.PassengerEmail);

                    // 1. Sinh payload bảo mật HMAC và mã QR
                    var securePayload = _qrCodeService.BuildSecureTicketPayload(
                        ticket.TicketCode,
                        ticket.BookingCode,
                        ticket.SeatNumber,
                        ticket.RouteName,
                        ticket.DepartureDate,
                        ticket.DepartureTime,
                        ticket.Price);

                    var payloadString = securePayload.ToPayloadString();
                    ticket.QrPayload = payloadString;

                    // Sinh ảnh QR PNG và chuỗi Base64
                    byte[] qrPngBytes = _qrCodeService.GenerateQrPngBytes(payloadString, pixelsPerModule: 12);
                    ticket.QrCodeBase64 = $"data:image/png;base64,{Convert.ToBase64String(qrPngBytes)}";

                    // 2. Render Template HTML Email
                    var htmlBody = _templateService.RenderTicketEmailHtml(ticket);

                    // 3. Chuẩn bị file đính kèm
                    var attachments = new List<EmailAttachment>();

                    // Đính kèm ảnh QR
                    if (task.AttachQrImage && qrPngBytes.Length > 0)
                    {
                        attachments.Add(new EmailAttachment
                        {
                            FileName = $"QR_CheckIn_{ticket.TicketCode}.png",
                            Data = qrPngBytes,
                            ContentType = "image/png"
                        });
                    }

                    // Tạo Scope riêng cho từng task gửi mail
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var pdfService = scope.ServiceProvider.GetRequiredService<ITicketPdfService>();
                        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                        // Đính kèm File PDF Vé điện tử
                        if (task.AttachPdf)
                        {
                            try
                            {
                                byte[] pdfBytes = await pdfService.GenerateTicketPdfAsync(ticket, qrPngBytes);
                                if (pdfBytes != null && pdfBytes.Length > 0)
                                {
                                    attachments.Add(new EmailAttachment
                                    {
                                        FileName = $"Ve_Xe_Dien_Tu_{ticket.TicketCode}.pdf",
                                        Data = pdfBytes,
                                        ContentType = "application/pdf"
                                    });
                                }
                            }
                            catch (Exception pdfEx)
                            {
                                _logger.LogWarning(pdfEx, "Không thể sinh file PDF vé {TicketCode}, email vẫn sẽ được gửi với ảnh QR đính kèm.", ticket.TicketCode);
                            }
                        }

                        // 4. Gửi email
                        var subject = $"[SmartBus Go] Xác nhận đặt vé thành công - Mã vé: {ticket.TicketCode} ({ticket.RouteName})";
                        var sendSuccess = await emailService.SendEmailAsync(
                            ticket.PassengerEmail,
                            ticket.PassengerName,
                            subject,
                            htmlBody,
                            attachments);

                        if (sendSuccess)
                        {
                            _logger.LogInformation("✓ Đã gửi vé thành công tới khách hàng {Email} cho vé #{TicketCode}", ticket.PassengerEmail, ticket.TicketCode);
                        }
                        else
                        {
                            _logger.LogWarning("Không gửi được email vé #{TicketCode}. Kiểm tra lại cấu hình SMTP.", ticket.TicketCode);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Đóng worker khi dừng ứng dụng
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi không mong muốn trong tiến trình TicketEmailBackgroundWorker");
                    await Task.Delay(2000, stoppingToken); // Chờ 2s trước khi tiếp tục
                }
            }

            _logger.LogInformation("=== SmartBus Ticket Email Worker đã dừng ===");
        }
    }
}

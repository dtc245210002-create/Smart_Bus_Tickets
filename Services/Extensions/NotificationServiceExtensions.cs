using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebApplication1.Services.BackgroundQueue;
using WebApplication1.Services.Notifications;
using WebApplication1.Services.Notifications.Models;
using WebApplication1.Services.Pdf;
using WebApplication1.Services.Qr;

namespace WebApplication1.Services.Extensions
{
    public static class NotificationServiceExtensions
    {
        /// <summary>
        /// Đăng ký toàn bộ hệ sinh thái dịch vụ Thông báo, QR Code, PDF và Background Queue
        /// Chỉ cần gọi 1 dòng duy nhất trong Program.cs: builder.Services.AddSmartBusNotificationSystem(builder.Configuration);
        /// </summary>
        public static IServiceCollection AddSmartBusNotificationSystem(this IServiceCollection services, IConfiguration configuration)
        {
            // Cấu hình Email
            services.Configure<EmailOptions>(configuration.GetSection("EmailSettings"));

            // Dịch vụ QR Code & Bảo mật
            services.AddSingleton<IQrCodeService, QrCodeService>();

            // Dịch vụ sinh file PDF vé điện tử (hỗ trợ tạo theo từng request/task)
            services.AddScoped<ITicketPdfService, TicketPdfService>();

            // Dịch vụ render template Email, SMS, Zalo ZNS
            services.AddSingleton<INotificationTemplateService, NotificationTemplateService>();

            // Dịch vụ gửi Email qua MailKit
            services.AddScoped<IEmailService, EmailService>();

            // Hàng đợi In-Memory Channel cho tác vụ gửi mail ngầm
            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();

            // Background Worker chạy ngầm theo vòng đời ứng dụng
            services.AddHostedService<TicketEmailBackgroundWorker>();

            return services;
        }
    }
}

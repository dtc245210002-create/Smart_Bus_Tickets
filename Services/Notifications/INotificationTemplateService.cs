using WebApplication1.Services.Notifications.Models;

namespace WebApplication1.Services.Notifications
{
    public interface INotificationTemplateService
    {
        /// <summary>
        /// Tạo nội dung HTML Email sang trọng chuẩn Responsive cho SmartBus Go
        /// </summary>
        string RenderTicketEmailHtml(TicketNotificationModel model);

        /// <summary>
        /// Tạo nội dung tin nhắn SMS Brandname ngắn gọn, chuẩn viễn thông
        /// </summary>
        string RenderTicketSmsText(TicketNotificationModel model);

        /// <summary>
        /// Tạo payload dữ liệu gửi qua Zalo Notification Service (Zalo ZNS)
        /// </summary>
        string RenderTicketZaloZnsJson(TicketNotificationModel model);
    }
}

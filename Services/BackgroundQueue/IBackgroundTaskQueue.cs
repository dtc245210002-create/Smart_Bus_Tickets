using System.Threading;
using System.Threading.Tasks;
using WebApplication1.Services.Notifications.Models;

namespace WebApplication1.Services.BackgroundQueue
{
    public class TicketEmailTask
    {
        public TicketNotificationModel TicketData { get; set; } = new();
        public bool AttachPdf { get; set; } = true;
        public bool AttachQrImage { get; set; } = true;
        public int RetryCount { get; set; } = 0;
    }

    public interface IBackgroundTaskQueue
    {
        /// <summary>
        /// Đẩy tác vụ gửi email vé vào hàng đợi ngầm (bất đồng bộ, không chặn luồng chính)
        /// </summary>
        ValueTask QueueTicketEmailAsync(TicketEmailTask task);

        /// <summary>
        /// Lấy tác vụ ra khỏi hàng đợi để xử lý (dùng bởi BackgroundService)
        /// </summary>
        ValueTask<TicketEmailTask> DequeueAsync(CancellationToken cancellationToken);
    }
}

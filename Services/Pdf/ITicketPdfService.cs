using System.Threading.Tasks;
using WebApplication1.Services.Notifications.Models;

namespace WebApplication1.Services.Pdf
{
    public interface ITicketPdfService
    {
        /// <summary>
        /// Tạo file PDF vé điện tử (dạng byte[]) kèm ảnh QR code để gửi đính kèm email
        /// </summary>
        Task<byte[]> GenerateTicketPdfAsync(TicketNotificationModel model, byte[] qrImageBytes);
    }
}

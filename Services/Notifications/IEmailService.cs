using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebApplication1.Services.Notifications
{
    public class EmailAttachment
    {
        public string FileName { get; set; } = string.Empty;
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public string ContentType { get; set; } = "application/octet-stream";
    }

    public interface IEmailService
    {
        /// <summary>
        /// Gửi email HTML có đính kèm file (PDF vé, ảnh QR)
        /// </summary>
        Task<bool> SendEmailAsync(
            string toEmail,
            string toName,
            string subject,
            string htmlBody,
            IEnumerable<EmailAttachment>? attachments = null);
    }
}

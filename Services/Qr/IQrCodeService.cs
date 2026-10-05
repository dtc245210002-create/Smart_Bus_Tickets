using System;
using WebApplication1.Services.Qr.Models;

namespace WebApplication1.Services.Qr
{
    /// <summary>
    /// Giao diện dịch vụ sinh mã QR an toàn
    /// </summary>
    public interface IQrCodeService
    {
        /// <summary>
        /// Tạo byte array ảnh PNG từ chuỗi dữ liệu bất kỳ
        /// </summary>
        byte[] GenerateQrPngBytes(string payload, int pixelsPerModule = 15);

        /// <summary>
        /// Tạo chuỗi Base64 Data URL (data:image/png;base64,...) dùng nhúng trực tiếp vào thẻ img HTML
        /// </summary>
        string GenerateQrBase64(string payload, int pixelsPerModule = 15);

        /// <summary>
        /// Tạo chuỗi SVG thuần (vector, nét căng mọi kích cỡ)
        /// </summary>
        string GenerateQrSvg(string payload, int pixelsPerModule = 15);

        /// <summary>
        /// Xây dựng payload vé an toàn kèm chữ ký HMAC chống làm giả vé
        /// </summary>
        SecureTicketPayload BuildSecureTicketPayload(
            string ticketCode,
            string bookingCode,
            string seatNumber,
            string routeCode,
            string departureDate,
            string departureTime,
            decimal price);

        /// <summary>
        /// Xác thực chuỗi payload QR có hợp lệ và chưa bị sửa đổi không
        /// </summary>
        bool ValidateTicketPayload(string payloadString, out string reason);
    }
}

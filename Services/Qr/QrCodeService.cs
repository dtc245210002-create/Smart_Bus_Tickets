using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QRCoder;
using WebApplication1.Services.Qr.Models;

namespace WebApplication1.Services.Qr
{
    public class QrCodeService : IQrCodeService
    {
        private readonly string _hmacSecretKey;
        private readonly ILogger<QrCodeService> _logger;

        public QrCodeService(IConfiguration configuration, ILogger<QrCodeService> logger)
        {
            _logger = logger;
            // Secret key bảo mật cho chữ ký HMAC của vé
            _hmacSecretKey = configuration["Security:TicketHmacSecret"] ?? "SmartBusGo_Secret_HMAC_Key_2026_TTCSK6N3_VerySecure!";
        }

        public byte[] GenerateQrPngBytes(string payload, int pixelsPerModule = 15)
        {
            if (string.IsNullOrEmpty(payload))
            {
                throw new ArgumentException("Payload không được để trống", nameof(payload));
            }

            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
            
            // PngByteQRCode sinh byte PNG cực nhanh mà không cần System.Drawing / GDI+
            var qrCode = new PngByteQRCode(qrCodeData);
            return qrCode.GetGraphic(pixelsPerModule);
        }

        public string GenerateQrBase64(string payload, int pixelsPerModule = 15)
        {
            var bytes = GenerateQrPngBytes(payload, pixelsPerModule);
            var base64 = Convert.ToBase64String(bytes);
            return $"data:image/png;base64,{base64}";
        }

        public string GenerateQrSvg(string payload, int pixelsPerModule = 15)
        {
            if (string.IsNullOrEmpty(payload))
            {
                throw new ArgumentException("Payload không được để trống", nameof(payload));
            }

            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
            var svgQrCode = new SvgQRCode(qrCodeData);
            return svgQrCode.GetGraphic(pixelsPerModule);
        }

        public SecureTicketPayload BuildSecureTicketPayload(
            string ticketCode,
            string bookingCode,
            string seatNumber,
            string routeCode,
            string departureDate,
            string departureTime,
            decimal price)
        {
            var rawDataToSign = $"{ticketCode}|{bookingCode}|{seatNumber}|{routeCode}|{departureDate}|{departureTime}|{price:0}";
            var checksum = ComputeHmacSha256(rawDataToSign, _hmacSecretKey);

            return new SecureTicketPayload
            {
                TicketCode = ticketCode,
                BookingCode = bookingCode,
                SeatNumber = seatNumber,
                RouteCode = routeCode,
                DepartureDate = departureDate,
                DepartureTime = departureTime,
                Price = price,
                Checksum = checksum,
                GeneratedAt = DateTime.UtcNow
            };
        }

        public bool ValidateTicketPayload(string payloadString, out string reason)
        {
            reason = string.Empty;
            if (string.IsNullOrWhiteSpace(payloadString) || !payloadString.StartsWith("SBG|"))
            {
                reason = "Chuỗi mã QR không đúng định dạng của SmartBus Go.";
                return false;
            }

            try
            {
                var parts = payloadString.Split('|');
                string ticketCode = "", bookingCode = "", seatNumber = "", routeCode = "", dep = "", price = "", checksum = "";

                foreach (var part in parts)
                {
                    if (part.StartsWith("TKT:")) ticketCode = part.Substring(4);
                    else if (part.StartsWith("BK:")) bookingCode = part.Substring(3);
                    else if (part.StartsWith("SEAT:")) seatNumber = part.Substring(5);
                    else if (part.StartsWith("RT:")) routeCode = part.Substring(3);
                    else if (part.StartsWith("DEP:")) dep = part.Substring(4);
                    else if (part.StartsWith("PRICE:")) price = part.Substring(6);
                    else if (part.StartsWith("CS:")) checksum = part.Substring(3);
                }

                if (string.IsNullOrEmpty(ticketCode) || string.IsNullOrEmpty(checksum))
                {
                    reason = "Thiếu thông tin bắt buộc trong mã vé.";
                    return false;
                }

                var depParts = dep.Split('_');
                var depDate = depParts.Length > 0 ? depParts[0] : "";
                var depTime = depParts.Length > 1 ? depParts[1] : "";

                decimal.TryParse(price, out var parsedPrice);
                var rawDataToSign = $"{ticketCode}|{bookingCode}|{seatNumber}|{routeCode}|{depDate}|{depTime}|{parsedPrice:0}";
                var expectedChecksum = ComputeHmacSha256(rawDataToSign, _hmacSecretKey);

                if (!string.Equals(checksum, expectedChecksum, StringComparison.OrdinalIgnoreCase))
                {
                    reason = "Chữ ký bảo mật không khớp! Vé có dấu hiệu bị giả mạo.";
                    return false;
                }

                reason = "Vé hợp lệ và chính chủ.";
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi giải mã payload QR");
                reason = "Lỗi khi xử lý dữ liệu mã QR.";
                return false;
            }
        }

        private static string ComputeHmacSha256(string data, string secretKey)
        {
            var keyBytes = Encoding.UTF8.GetBytes(secretKey);
            var dataBytes = Encoding.UTF8.GetBytes(data);

            using var hmac = new HMACSHA256(keyBytes);
            var hashBytes = hmac.ComputeHash(dataBytes);
            // Lấy 12 ký tự hex đầu làm checksum ngắn gọn, dễ quét
            return Convert.ToHexString(hashBytes).Substring(0, 12);
        }
    }
}

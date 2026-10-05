using System;

namespace WebApplication1.Services.Qr.Models
{
    /// <summary>
    /// Model chứa dữ liệu mã hóa an toàn của vé để nhúng vào QR Code
    /// </summary>
    public class SecureTicketPayload
    {
        public string TicketCode { get; set; } = string.Empty;
        public string BookingCode { get; set; } = string.Empty;
        public string SeatNumber { get; set; } = string.Empty;
        public string RouteCode { get; set; } = string.Empty;
        public string DepartureDate { get; set; } = string.Empty;
        public string DepartureTime { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Checksum { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Chuyển thành chuỗi payload chuẩn SmartBus
        /// </summary>
        public string ToPayloadString()
        {
            return $"SBG|TKT:{TicketCode}|BK:{BookingCode}|SEAT:{SeatNumber}|RT:{RouteCode}|DEP:{DepartureDate}_{DepartureTime}|PRICE:{Price:0}|CS:{Checksum}";
        }
    }
}

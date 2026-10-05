using System;

namespace WebApplication1.Services.Notifications.Models
{
    /// <summary>
    /// Model chứa toàn bộ dữ liệu cần thiết để tạo Email, SMS, Zalo ZNS và PDF
    /// </summary>
    public class TicketNotificationModel
    {
        public string TicketCode { get; set; } = string.Empty;
        public string BookingCode { get; set; } = string.Empty;
        public string PassengerName { get; set; } = "Hành khách";
        public string PassengerEmail { get; set; } = string.Empty;
        public string PassengerPhone { get; set; } = string.Empty;

        public string RouteName { get; set; } = string.Empty;
        public string StartPoint { get; set; } = string.Empty;
        public string EndPoint { get; set; } = string.Empty;

        public string DepartureDate { get; set; } = string.Empty;
        public string DepartureTime { get; set; } = string.Empty;
        public string SeatNumber { get; set; } = string.Empty;
        public string BusTypeName { get; set; } = string.Empty;
        public string LicensePlate { get; set; } = string.Empty;

        public string BoardingStopName { get; set; } = string.Empty;
        public string BoardingStopAddress { get; set; } = string.Empty;
        public string DropOffStopName { get; set; } = string.Empty;
        public string DropOffStopAddress { get; set; } = string.Empty;

        public decimal Price { get; set; }
        public string WebTicketUrl { get; set; } = string.Empty;
        public string QrCodeBase64 { get; set; } = string.Empty;
        public string QrPayload { get; set; } = string.Empty;
    }
}

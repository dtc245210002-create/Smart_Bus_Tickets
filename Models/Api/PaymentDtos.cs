using System;
using System.Collections.Generic;

namespace WebApplication1.Models.Api
{
    public class VerifyPaymentRequest
    {
        public string? BookingCode { get; set; }
        public string? TicketCode { get; set; }
        public decimal? Amount { get; set; }
        public string? PaymentMethod { get; set; } = "VietQR";
        public string? TransactionCode { get; set; }
        public int? TripId { get; set; }
        public List<string>? SeatNumbers { get; set; }
    }

    public class VerifyPaymentResponse
    {
        public bool Success { get; set; }
        public string TransactionCode { get; set; } = string.Empty;
        public string BookingCode { get; set; } = string.Empty;
        public string TicketCode { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string Status { get; set; } = "Paid";
        public DateTime PaymentTime { get; set; }
        public string RouteName { get; set; } = string.Empty;
        public string SeatNumber { get; set; } = string.Empty;
        public string QrPayload { get; set; } = string.Empty;
    }
}

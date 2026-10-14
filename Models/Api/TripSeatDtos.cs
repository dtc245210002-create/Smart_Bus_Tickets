using System;
using System.Collections.Generic;

namespace WebApplication1.Models.Api
{
    public class TripSeatMapDto
    {
        public int TripId { get; set; }
        public string RouteName { get; set; } = string.Empty;
        public string Origin { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public string TripDate { get; set; } = string.Empty;
        public string DepartureTime { get; set; } = string.Empty;
        public string ArrivalTime { get; set; } = string.Empty;
        public string BusTypeName { get; set; } = string.Empty;
        public string LicensePlate { get; set; } = string.Empty;
        public int TotalSeats { get; set; }
        public int AvailableSeats { get; set; }
        public int BookedSeats { get; set; }
        public int HeldSeats { get; set; }
        public decimal BasePrice { get; set; }
        public int Floors { get; set; } = 1;
        public List<SeatDto> Seats { get; set; } = new();
    }

    public class SeatDto
    {
        public string SeatNumber { get; set; } = string.Empty;
        public int Floor { get; set; } = 1;
        public int Row { get; set; }
        public string Column { get; set; } = string.Empty;
        public string Type { get; set; } = "Standard"; // Standard, VIP, Sleeper
        public decimal Price { get; set; }
        public string Status { get; set; } = "AVAILABLE"; // AVAILABLE, BOOKED, HOLD
        public bool IsMine { get; set; } = false;
        public DateTime? HeldExpiresAt { get; set; }
        public int? RemainingHoldSeconds { get; set; }
    }
}

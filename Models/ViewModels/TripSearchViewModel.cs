using System;
using System.Collections.Generic;

namespace WebApplication1.Models.ViewModels
{
    public class TripSearchViewModel
    {
        public string From { get; set; } = "Hà Nội";
        public string To { get; set; } = "Hải Phòng";
        public DateTime DepartureDate { get; set; } = DateTime.Today;
        public DateTime? ReturnDate { get; set; }
        public string? SortBy { get; set; } = "default";
        public string? BusTypeFilter { get; set; }
        public string? TimeOfDayFilter { get; set; }

        public string? ErrorMessage { get; set; }
        public List<TripItemViewModel> Trips { get; set; } = new();
        public int TotalTrips => Trips.Count;
    }

    public class TripItemViewModel
    {
        public int TripId { get; set; }
        public string OperatorName { get; set; } = "Anh Huy Travel";
        public double Rating { get; set; } = 4.8;
        public int ReviewCount { get; set; } = 1398;
        public string BusTypeName { get; set; } = "Limousine VIP 16 Chỗ";
        public string BusImage { get; set; } = "https://images.unsplash.com/photo-1544620347-c4fd4a3d5957?w=400&q=80";
        public string LicensePlate { get; set; } = "29B-888.68";

        public TimeSpan DepartureTime { get; set; }
        public string DeparturePoint { get; set; } = string.Empty;
        public TimeSpan ArrivalTime { get; set; }
        public string ArrivalPoint { get; set; } = string.Empty;
        public string DurationText { get; set; } = "2h 10m";

        public decimal Price { get; set; }
        public decimal? OriginalPrice { get; set; }
        public int AvailableSeats { get; set; }
        public int TotalCapacity { get; set; }

        public bool HasGps { get; set; } = true;
        public bool IsFlashSale { get; set; } = true;
        public string FlashSaleText { get; set; } = "FLASH SALE 50%";
        public string NoticeText { get; set; } = string.Empty;

        public List<string> BoardingPoints { get; set; } = new();
        public List<string> DropOffPoints { get; set; } = new();
        public List<SeatItemViewModel> Seats { get; set; } = new();
    }

    public class SeatItemViewModel
    {
        public string SeatCode { get; set; } = string.Empty;
        public int Floor { get; set; } = 1;
        public decimal Price { get; set; }
        public string Status { get; set; } = "AVAILABLE"; // AVAILABLE, BOOKED
    }
}

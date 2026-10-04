using System;
using System.Collections.Generic;

namespace WebApplication1.Models.Api
{
    public class RouteSearchQuery
    {
        public string? Origin { get; set; }
        public string? From { get; set; }
        public string? Destination { get; set; }
        public string? To { get; set; }
        public DateTime? Date { get; set; }
        public string? Sort { get; set; }
        public string? BusType { get; set; }
    }

    public class RouteSearchResultDto
    {
        public string Origin { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public string SearchDate { get; set; } = string.Empty;
        public int TotalTrips { get; set; }
        public List<TripSummaryDto> Trips { get; set; } = new();
    }

    public class TripSummaryDto
    {
        public int TripId { get; set; }
        public int RouteId { get; set; }
        public string RouteName { get; set; } = string.Empty;
        public string Origin { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public string TripDate { get; set; } = string.Empty;
        public string DepartureTime { get; set; } = string.Empty;
        public string ArrivalTime { get; set; } = string.Empty;
        public string DurationText { get; set; } = string.Empty;
        public string BusTypeName { get; set; } = string.Empty;
        public string LicensePlate { get; set; } = string.Empty;
        public string OperatorName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? OriginalPrice { get; set; }
        public int AvailableSeats { get; set; }
        public int TotalSeats { get; set; }
        public bool IsFlashSale { get; set; }
        public string? FlashSaleText { get; set; }
        public List<string> BoardingPoints { get; set; } = new();
        public List<string> DropOffPoints { get; set; } = new();
    }
}

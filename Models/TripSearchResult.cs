namespace WebApplication1.Models
{
    public class TripSearchResult
    {
        public int TripId { get; set; }
        public string RouteCode { get; set; } = string.Empty;
        public string RouteName { get; set; } = string.Empty;
        public string FromStop { get; set; } = string.Empty;
        public string ToStop { get; set; } = string.Empty;
        public DateTime TripDate { get; set; }
        public TimeSpan DepartureTime { get; set; }
        public TimeSpan? ArrivalTime { get; set; }
        public string LicensePlate { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public int SoldTickets { get; set; }
        public int AvailableSeats { get; set; }
    }
}

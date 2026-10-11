namespace WebApplication1.Models.Entities
{
    public class Route
    {
        public int RouteId { get; set; }
        public string RouteCode { get; set; } = string.Empty;
        public string RouteName { get; set; } = string.Empty;
        public string StartPoint { get; set; } = string.Empty;
        public string EndPoint { get; set; } = string.Empty;
        public decimal? Distance { get; set; }
        public int? EstimatedDuration { get; set; }
        public string? Status { get; set; }

        public virtual ICollection<RouteStop> RouteStops { get; set; } = new List<RouteStop>();
        public virtual ICollection<Trip> Trips { get; set; } = new List<Trip>();
        public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
    }
}

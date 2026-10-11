namespace WebApplication1.Models.Entities
{
    public class BusStop
    {
        public int StopId { get; set; }
        public string StopName { get; set; } = string.Empty;
        public string? Address { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        public virtual ICollection<RouteStop> RouteStops { get; set; } = new List<RouteStop>();
    }
}

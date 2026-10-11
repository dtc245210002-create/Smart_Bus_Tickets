namespace WebApplication1.Models.Entities
{
    public class RouteStop
    {
        public int RouteStopId { get; set; }
        public int RouteId { get; set; }
        public int StopId { get; set; }
        public int StopOrder { get; set; }
        public TimeSpan? ArrivalTime { get; set; }
        public TimeSpan? DepartureTime { get; set; }

        public virtual Route? Route { get; set; }
        public virtual BusStop? BusStop { get; set; }
    }
}

namespace WebApplication1.Models.Entities
{
    public class Trip
    {
        public int TripId { get; set; }
        public int RouteId { get; set; }
        public int BusId { get; set; }
        public int DriverId { get; set; }
        public int? ScheduleId { get; set; }
        public DateTime TripDate { get; set; }
        public TimeSpan DepartureTime { get; set; }
        public TimeSpan? ArrivalTime { get; set; }
        public string? Status { get; set; }

        public virtual Route? Route { get; set; }
        public virtual Bus? Bus { get; set; }
        public virtual Driver? Driver { get; set; }
        public virtual Schedule? Schedule { get; set; }
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public virtual ICollection<VehicleLocation> VehicleLocations { get; set; } = new List<VehicleLocation>();
    }
}

namespace WebApplication1.Models.Entities
{
    public class Schedule
    {
        public int ScheduleId { get; set; }
        public int RouteId { get; set; }
        public TimeSpan DepartureTime { get; set; }
        public int DayOfWeek { get; set; }
        public string? Status { get; set; }

        public virtual Route? Route { get; set; }
        public virtual ICollection<Trip> Trips { get; set; } = new List<Trip>();
    }
}

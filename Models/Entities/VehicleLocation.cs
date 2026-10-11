namespace WebApplication1.Models.Entities
{
    public class VehicleLocation
    {
        public long LocationId { get; set; }
        public int TripId { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public decimal? Speed { get; set; }
        public DateTime RecordedAt { get; set; }

        public virtual Trip? Trip { get; set; }
    }
}

namespace WebApplication1.Models.Entities
{
    public class Driver
    {
        public int DriverId { get; set; }
        public int UserId { get; set; }
        public string LicenseNo { get; set; } = string.Empty;
        public DateOnly? LicenseExpiryDate { get; set; }
        public string? Status { get; set; }

        public virtual User? User { get; set; }
        public virtual ICollection<Trip> Trips { get; set; } = new List<Trip>();
    }
}

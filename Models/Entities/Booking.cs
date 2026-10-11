namespace WebApplication1.Models.Entities
{
    public class Booking
    {
        public int BookingId { get; set; }
        public int UserId { get; set; }
        public int TripId { get; set; }
        public string BookingCode { get; set; } = string.Empty;
        public DateTime BookingTime { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Status { get; set; }

        public virtual User? User { get; set; }
        public virtual Trip? Trip { get; set; }
        public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}

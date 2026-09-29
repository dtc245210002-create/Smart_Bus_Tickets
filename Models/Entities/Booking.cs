using System;
using System.Collections.Generic;

namespace WebApplication1.Models.Entities;

public partial class Booking
{
    public int BookingId { get; set; }

    public int UserId { get; set; }

    public int TripId { get; set; }

    public string BookingCode { get; set; } = null!;

    public DateTime BookingTime { get; set; }

    public decimal TotalAmount { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

    public virtual Trip Trip { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}

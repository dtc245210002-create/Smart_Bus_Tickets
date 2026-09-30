using System;
using System.Collections.Generic;

namespace WebApplication1.Models.Entities;

public partial class Ticket
{
    public int TicketId { get; set; }

    public int BookingId { get; set; }

    public string TicketCode { get; set; } = null!;

    public string? SeatNumber { get; set; }

    public decimal Price { get; set; }

    public int? BoardingStopId { get; set; }

    public int? DropOffStopId { get; set; }

    public string? Status { get; set; }

    public virtual BusStop? BoardingStop { get; set; }

    public virtual Booking Booking { get; set; } = null!;

    public virtual BusStop? DropOffStop { get; set; }
}

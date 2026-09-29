using System;
using System.Collections.Generic;

namespace WebApplication1.Models.Entities;

public partial class BusStop
{
    public int StopId { get; set; }

    public string StopName { get; set; } = null!;

    public string? Address { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public virtual ICollection<RouteStop> RouteStops { get; set; } = new List<RouteStop>();

    public virtual ICollection<Ticket> TicketBoardingStops { get; set; } = new List<Ticket>();

    public virtual ICollection<Ticket> TicketDropOffStops { get; set; } = new List<Ticket>();
}

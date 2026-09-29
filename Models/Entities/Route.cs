using System;
using System.Collections.Generic;

namespace WebApplication1.Models.Entities;

public partial class Route
{
    public int RouteId { get; set; }

    public string RouteCode { get; set; } = null!;

    public string RouteName { get; set; } = null!;

    public string StartPoint { get; set; } = null!;

    public string EndPoint { get; set; } = null!;

    public decimal? Distance { get; set; }

    public int? EstimatedDuration { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<RouteStop> RouteStops { get; set; } = new List<RouteStop>();

    public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();

    public virtual ICollection<Trip> Trips { get; set; } = new List<Trip>();
}

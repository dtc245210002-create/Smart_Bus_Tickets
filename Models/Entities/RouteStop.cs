using System;
using System.Collections.Generic;

namespace WebApplication1.Models.Entities;

public partial class RouteStop
{
    public int RouteStopId { get; set; }

    public int RouteId { get; set; }

    public int StopId { get; set; }

    public int StopOrder { get; set; }

    public TimeOnly? ArrivalTime { get; set; }

    public TimeOnly? DepartureTime { get; set; }

    public virtual Route Route { get; set; } = null!;

    public virtual BusStop Stop { get; set; } = null!;
}

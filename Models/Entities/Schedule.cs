using System;
using System.Collections.Generic;

namespace WebApplication1.Models.Entities;

public partial class Schedule
{
    public int ScheduleId { get; set; }

    public int RouteId { get; set; }

    public TimeOnly DepartureTime { get; set; }

    public int DayOfWeek { get; set; }

    public string? Status { get; set; }

    public virtual Route Route { get; set; } = null!;

    public virtual ICollection<Trip> Trips { get; set; } = new List<Trip>();
}

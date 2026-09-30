using System;
using System.Collections.Generic;

namespace WebApplication1.Models.Entities;

public partial class Trip
{
    public int TripId { get; set; }

    public int RouteId { get; set; }

    public int BusId { get; set; }

    public int DriverId { get; set; }

    public int? ScheduleId { get; set; }

    public DateOnly TripDate { get; set; }

    public TimeOnly DepartureTime { get; set; }

    public TimeOnly? ArrivalTime { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual Bus Bus { get; set; } = null!;

    public virtual Driver Driver { get; set; } = null!;

    public virtual Route Route { get; set; } = null!;

    public virtual Schedule? Schedule { get; set; }

    public virtual ICollection<VehicleLocation> VehicleLocations { get; set; } = new List<VehicleLocation>();
}

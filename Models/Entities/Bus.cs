using System;
using System.Collections.Generic;

namespace WebApplication1.Models.Entities;

public partial class Bus
{
    public int BusId { get; set; }

    public int BusTypeId { get; set; }

    public string LicensePlate { get; set; } = null!;

    public int Capacity { get; set; }

    public string? Status { get; set; }

    public virtual BusType BusType { get; set; } = null!;

    public virtual ICollection<Trip> Trips { get; set; } = new List<Trip>();

    // Tài xế được phân công cố định cho xe này (do Admin gán)
    public virtual Driver? AssignedDriver { get; set; }
}

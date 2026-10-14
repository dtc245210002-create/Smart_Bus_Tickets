using System;
using System.Collections.Generic;

namespace WebApplication1.Models.Entities;

public partial class Driver
{
    public int DriverId { get; set; }

    public int UserId { get; set; }

    public string LicenseNo { get; set; } = null!;

    public DateOnly? LicenseExpiryDate { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? Gender { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<Trip> Trips { get; set; } = new List<Trip>();

    public virtual User User { get; set; } = null!;
}

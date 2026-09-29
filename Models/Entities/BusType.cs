using System;
using System.Collections.Generic;

namespace WebApplication1.Models.Entities;

public partial class BusType
{
    public int BusTypeId { get; set; }

    public string TypeName { get; set; } = null!;

    public int Capacity { get; set; }

    public string? Description { get; set; }

    public virtual ICollection<Bus> Buses { get; set; } = new List<Bus>();
}

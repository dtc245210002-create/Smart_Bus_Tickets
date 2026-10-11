namespace WebApplication1.Models.Entities
{
    public class Bus
    {
        public int BusId { get; set; }
        public int BusTypeId { get; set; } // Đã sửa từ TypeId thành BusTypeId khớp SQL Server
        public string LicensePlate { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public string? Status { get; set; }

        public virtual BusType? BusType { get; set; }
        public virtual ICollection<Trip> Trips { get; set; } = new List<Trip>();
    }
}

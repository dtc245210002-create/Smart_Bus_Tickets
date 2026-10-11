namespace WebApplication1.Models.Entities
{
    public class BusType
    {
        public int BusTypeId { get; set; }
        public string TypeName { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public string? Description { get; set; }

        public virtual ICollection<Bus> Buses { get; set; } = new List<Bus>();
    }
}

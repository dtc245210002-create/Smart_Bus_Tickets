using System.Collections.Generic;

namespace WebApplication1.Models.BusLayout
{
    /// <summary>
    /// Định danh phân loại ghế hoặc tiện ích trên xe
    /// </summary>
    public enum SeatType
    {
        RegularSeat,    // Ghế ngồi tiêu chuẩn
        VipSeat,        // Ghế ngồi VIP
        SleeperLower,   // Giường nằm tầng dưới
        SleeperUpper,   // Giường nằm tầng trên
        VipCabin,       // Phòng Limousine riêng tư
        Aisle,          // Lối đi (khoảng trống)
        Driver,         // Vị trí tài xế
        Door            // Cửa lên xuống
    }

    /// <summary>
    /// Vị trí chi tiết của một ghế hoặc vị trí trên sơ đồ xe
    /// </summary>
    public class BusSeatPosition
    {
        public string SeatCode { get; set; } = string.Empty;
        public int Floor { get; set; } = 1; // 1: Tầng 1 (hoặc tầng dưới), 2: Tầng 2 (tầng trên)
        public int Row { get; set; }
        public int Col { get; set; }
        public SeatType Type { get; set; } = SeatType.RegularSeat;
        public decimal Price { get; set; }
        public string Status { get; set; } = "AVAILABLE"; // AVAILABLE, BOOKED, SELECTED
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsBookable => Type != SeatType.Aisle && Type != SeatType.Driver && Type != SeatType.Door;
    }

    /// <summary>
    /// Sơ đồ mặt sàn của một tầng xe
    /// </summary>
    public class BusFloorLayout
    {
        public int FloorNumber { get; set; } = 1;
        public string FloorName { get; set; } = "Tầng 1 (Tầng dưới)";
        public int TotalRows { get; set; }
        public int TotalCols { get; set; }
        public List<BusSeatPosition> Seats { get; set; } = new();
    }

    /// <summary>
    /// Cấu hình tổng thể của một loại xe (29 chỗ, 45 chỗ, giường nằm 2 tầng...)
    /// </summary>
    public class BusLayoutConfig
    {
        public string BusTypeCode { get; set; } = string.Empty; // SEAT_29, SEAT_45, SLEEPER_34, LIMOUSINE_22
        public string TypeName { get; set; } = string.Empty;
        public int TotalFloors { get; set; } = 1;
        public int Capacity { get; set; }
        public string Description { get; set; } = string.Empty;
        public string IconClass { get; set; } = "bi-bus-front";
        public List<BusFloorLayout> Floors { get; set; } = new();
    }
}

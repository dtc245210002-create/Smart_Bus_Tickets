namespace WebApplication1.Models.DTOs
{
    /// <summary>
    /// Yêu cầu quét và xác thực mã QR vé xe
    /// </summary>
    public class ValidateQrRequest
    {
        /// <summary>
        /// Chuỗi dữ liệu quét được từ mã QR (có thể là chuỗi định dạng SMARTBUS|..., mã vé trực tiếp hoặc link)
        /// </summary>
        public string QrPayload { get; set; } = string.Empty;

        /// <summary>
        /// (Tùy chọn) ID chuyến xe hiện tại mà nhân viên/tài xế đang phụ trách để đối soát chéo
        /// </summary>
        public int? CurrentTripId { get; set; }

        /// <summary>
        /// (Tùy chọn) Vĩ độ GPS lúc quét vé
        /// </summary>
        public decimal? Latitude { get; set; }

        /// <summary>
        /// (Tùy chọn) Kinh độ GPS lúc quét vé
        /// </summary>
        public decimal? Longitude { get; set; }

        /// <summary>
        /// (Tùy chọn) Ghi chú thiết bị hoặc mã thiết bị quét
        /// </summary>
        public string? DeviceInfo { get; set; }
    }

    /// <summary>
    /// Kết quả phản hồi chung của API xác thực vé QR
    /// </summary>
    public class ValidateQrResponse
    {
        public bool Success { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public TicketValidationData? Data { get; set; }
    }

    /// <summary>
    /// Chi tiết thông tin vé, hành khách và chuyến xe sau khi xác thực
    /// </summary>
    public class TicketValidationData
    {
        public int TicketId { get; set; }
        public string TicketCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string SeatNumber { get; set; } = string.Empty;
        public decimal Price { get; set; }

        // Thông tin Đặt vé
        public int BookingId { get; set; }
        public string BookingCode { get; set; } = string.Empty;
        public DateTime BookingTime { get; set; }

        // Thông tin Hành khách
        public PassengerDto Passenger { get; set; } = new();

        // Thông tin Chuyến xe
        public TripDto Trip { get; set; } = new();

        // Điểm đón & trả
        public string BoardingStop { get; set; } = string.Empty;
        public string? BoardingStopAddress { get; set; }
        public string DropOffStop { get; set; } = string.Empty;
        public string? DropOffStopAddress { get; set; }

        // Thời gian xác thực check-in lên xe
        public DateTime CheckInTime { get; set; } = DateTime.Now;
    }

    public class PassengerDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
    }

    public class TripDto
    {
        public int TripId { get; set; }
        public string RouteCode { get; set; } = string.Empty;
        public string RouteName { get; set; } = string.Empty;
        public string StartPoint { get; set; } = string.Empty;
        public string EndPoint { get; set; } = string.Empty;
        public DateOnly TripDate { get; set; }
        public TimeSpan DepartureTime { get; set; }
        public TimeSpan? ArrivalTime { get; set; }
        public string LicensePlate { get; set; } = string.Empty;
        public string BusTypeName { get; set; } = string.Empty;
        public string DriverName { get; set; } = string.Empty;
        public string? DriverPhone { get; set; }
    }

    /// <summary>
    /// Các mã trạng thái phản hồi chuẩn hóa của việc soát vé
    /// </summary>
    public static class TicketValidationCodes
    {
        public const string Success = "VALID_TICKET";
        public const string InvalidInput = "INVALID_INPUT";
        public const string TicketNotFound = "TICKET_NOT_FOUND";
        public const string AlreadyUsed = "ALREADY_USED";
        public const string TicketCancelled = "TICKET_CANCELLED";
        public const string InvalidStatus = "INVALID_STATUS";
        public const string TripMismatch = "TRIP_MISMATCH";
        public const string ExpiredTrip = "EXPIRED_TRIP";
    }
}

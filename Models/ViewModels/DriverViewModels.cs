using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models.ViewModels
{
    /// <summary>
    /// ViewModel dùng cho form đăng nhập của Bác tài (Tài xế)
    /// </summary>
    public class DriverLoginViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập Mã tài xế, Bằng lái, SĐT hoặc Email")]
        [Display(Name = "Mã tài xế / Bằng lái / Số điện thoại")]
        public string DriverIdentifier { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu nội bộ")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu nội bộ")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Ghi nhớ phiên làm việc")]
        public bool RememberMe { get; set; } = true;
    }

    /// <summary>
    /// ViewModel Bảng điều khiển ca làm việc dành cho Bác tài
    /// </summary>
    public class DriverDashboardViewModel
    {
        // Thông tin tài xế
        public int DriverId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string LicenseNo { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";

        // Thống kê ca làm việc
        public int TodayTripsCount { get; set; }
        public int TotalPassengersCount { get; set; }

        // Danh sách các chuyến xe được giao
        public List<DriverTripItemViewModel> AssignedTrips { get; set; } = new();
    }

    /// <summary>
    /// ViewModel thông tin chuyến xe được phân công cho tài xế
    /// </summary>
    public class DriverTripItemViewModel
    {
        public int TripId { get; set; }
        public string RouteName { get; set; } = string.Empty;
        public string StartPoint { get; set; } = string.Empty;
        public string EndPoint { get; set; } = string.Empty;
        public DateOnly TripDate { get; set; }
        public TimeOnly DepartureTime { get; set; }
        public TimeOnly? ArrivalTime { get; set; }
        public string LicensePlate { get; set; } = string.Empty;
        public string BusTypeName { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public int BookedSeats { get; set; }
        public string Status { get; set; } = "ACTIVE";
    }

    /// <summary>
    /// Kết quả xác thực / soát vé bằng mã QR hoặc mã vé
    /// </summary>
    public class DriverVerifyTicketResultViewModel
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string TicketCode { get; set; } = string.Empty;
        public string PassengerName { get; set; } = string.Empty;
        public string PassengerPhone { get; set; } = string.Empty;
        public string SeatNumber { get; set; } = string.Empty;
        public string RouteName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}

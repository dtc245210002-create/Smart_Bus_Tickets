using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập Email hoặc Số điện thoại")]
        [Display(Name = "Email hoặc Số điện thoại")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Ghi nhớ đăng nhập")]
        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
        [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ")]
        [Display(Name = "Địa chỉ Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [RegularExpression(@"^(0[3|5|7|8|9])+([0-9]{8})$", ErrorMessage = "Số điện thoại không hợp lệ (10 chữ số, đầu 03, 05, 07, 08, 09)")]
        [Display(Name = "Số điện thoại")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu")]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Range(typeof(bool), "true", "true", ErrorMessage = "Bạn cần đồng ý với điều khoản dịch vụ của SmartBus Go")]
        public bool AcceptTerms { get; set; }
    }

    public class VerifyOtpViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập đủ 6 chữ số mã xác thực")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Mã xác thực gồm 6 chữ số")]
        [RegularExpression(@"^[0-9]{6}$", ErrorMessage = "Mã OTP chỉ bao gồm các chữ số")]
        public string OtpCode { get; set; } = string.Empty;
    }

    public class DriverLoginViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập Mã tài xế hoặc Số điện thoại")]
        [Display(Name = "Mã tài xế / Số điện thoại")]
        public string DriverIdentifier { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu nội bộ")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Ghi nhớ phiên làm việc")]
        public bool RememberMe { get; set; }
    }

    public class TicketDetailViewModel
    {
        // Thông tin vé (Ticket)
        public int TicketId { get; set; }
        public string TicketCode { get; set; } = string.Empty;
        public string SeatNumber { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Status { get; set; } = "ACTIVE"; // ACTIVE, USED, EXPIRED, CANCELLED

        // Thông tin đặt chỗ (Booking)
        public int BookingId { get; set; }
        public string BookingCode { get; set; } = string.Empty;
        public DateTime BookingTime { get; set; }
        public decimal TotalAmount { get; set; }

        // Thông tin hành khách (User)
        public string PassengerName { get; set; } = string.Empty;
        public string PassengerPhone { get; set; } = string.Empty;
        public string PassengerEmail { get; set; } = string.Empty;

        // Thông tin tuyến đường (Route)
        public string RouteCode { get; set; } = string.Empty;
        public string RouteName { get; set; } = string.Empty;
        public string StartPoint { get; set; } = string.Empty;
        public string EndPoint { get; set; } = string.Empty;
        public decimal Distance { get; set; }
        public int EstimatedDuration { get; set; } // Phút

        // Thông tin điểm đón & trả (BusStop)
        public string BoardingStopName { get; set; } = string.Empty;
        public string BoardingStopAddress { get; set; } = string.Empty;
        public TimeSpan DepartureTime { get; set; }

        public string DropOffStopName { get; set; } = string.Empty;
        public string DropOffStopAddress { get; set; } = string.Empty;
        public TimeSpan? ArrivalTime { get; set; }

        // Thông tin chuyến xe & Xe buýt (Trip, Bus, BusType)
        public int TripId { get; set; }
        public DateTime TripDate { get; set; }
        public string LicensePlate { get; set; } = string.Empty;
        public string BusTypeName { get; set; } = string.Empty;
        public string DriverName { get; set; } = string.Empty;
        public string DriverPhone { get; set; } = string.Empty;

        // Dữ liệu mã hóa QR (chứa token hoặc chuỗi xác thực)
        public string QrDataPayload { get; set; } = string.Empty;
    }
}

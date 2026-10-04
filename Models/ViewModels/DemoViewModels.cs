using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models.ViewModels;

public class CustomerProfileViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên"), StringLength(100)]
    public string FullName { get; set; } = "";
    [Required, EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = "";
    [Required, RegularExpression(@"^0[35789]\d{8}$", ErrorMessage = "Số điện thoại phải có 10 chữ số hợp lệ")]
    public string Phone { get; set; } = "";
    [DataType(DataType.Date)] public DateTime? BirthDate { get; set; }
    [StringLength(250)] public string? Address { get; set; }
    public string DiscountGroup { get; set; } = "Thông thường";
    public string VerificationStatus { get; set; } = "Chưa gửi hồ sơ";
}

public class DemoPaymentViewModel
{
    public string BookingCode { get; set; } = "";
    public TripItemViewModel Trip { get; set; } = new();
    public DateTime TravelDate { get; set; }
    public List<string> Seats { get; set; } = [];
    public DateTimeOffset ExpiresAt { get; set; }
    public string Status { get; set; } = "HOLD";
    public string PassengerName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public string BoardingPoint { get; set; } = "";
    public string DropOffPoint { get; set; } = "";
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Total => Subtotal - Discount;
    public string? Voucher { get; set; }
    public string? PaymentMethod { get; set; }
    public List<string> TicketCodes { get; set; } = [];
}

public class TicketChangeViewModel
{
    public DemoPaymentViewModel Booking { get; set; } = new();
    public List<TripItemViewModel> Alternatives { get; set; } = [];
    public DateTime NewDate { get; set; }
}

public class DriverDashboardViewModel
{
    public List<DemoPaymentViewModel> Bookings { get; set; } = [];
    public List<string> Incidents { get; set; } = [];
}

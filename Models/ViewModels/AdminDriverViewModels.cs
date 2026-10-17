using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models.ViewModels
{
    /// <summary>
    /// ViewModel hiển thị danh sách tài xế và xe buýt cố định cho Admin quản lý
    /// </summary>
    public class AdminDriverListItemViewModel
    {
        public int DriverId { get; set; }
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string LicenseNo { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";

        // Thông tin xe buýt cố định (1 tài xế chỉ quản lý 1 xe)
        public int? AssignedBusId { get; set; }
        public string? AssignedBusPlate { get; set; }
        public string? AssignedBusType { get; set; }
        public int? AssignedBusCapacity { get; set; }
    }

    /// <summary>
    /// Yêu cầu phân công xe cố định cho tài xế
    /// </summary>
    public class AssignBusRequest
    {
        [Required]
        public int DriverId { get; set; }

        public int? BusId { get; set; }
    }

    /// <summary>
    /// Yêu cầu Admin cấp tài khoản và mật khẩu mới cho Bác tài
    /// </summary>
    public class CreateDriverAccountRequest
    {
        [Required(ErrorMessage = "Họ và tên bác tài là bắt buộc")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu khởi tạo là bắt buộc")]
        public string Password { get; set; } = "123456";

        [Required(ErrorMessage = "Số giấy phép lái xe là bắt buộc")]
        public string LicenseNo { get; set; } = string.Empty;

        public int? AssignedBusId { get; set; }
    }

    /// <summary>
    /// Danh sách xe buýt phục vụ chọn phân công
    /// </summary>
    public class AdminBusOptionViewModel
    {
        public int BusId { get; set; }
        public string LicensePlate { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public bool IsAssigned { get; set; }
        public string? AssignedDriverName { get; set; }
    }
}

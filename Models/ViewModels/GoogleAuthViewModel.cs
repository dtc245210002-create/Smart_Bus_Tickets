using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models.ViewModels
{
    /// <summary>
    /// Model dữ liệu truyền vào khi xác thực tài khoản Google / Gmail
    /// </summary>
    public class GoogleLoginInputModel
    {
        [Required(ErrorMessage = "Vui lòng nhập địa chỉ Gmail")]
        [EmailAddress(ErrorMessage = "Địa chỉ Gmail không hợp lệ")]
        [Display(Name = "Địa chỉ Gmail")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập họ và tên của bạn")]
        [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự")]
        [Display(Name = "Họ và tên hiển thị")]
        public string FullName { get; set; } = string.Empty;

        public string? AvatarUrl { get; set; }

        public string? ReturnUrl { get; set; }
    }

    /// <summary>
    /// ViewModel dùng cho giao diện chọn tài khoản Google (Google Account Chooser)
    /// </summary>
    public class GoogleAccountChoiceViewModel
    {
        public string ReturnUrl { get; set; } = string.Empty;

        // Danh sách tài khoản Google gợi ý sẵn để người dùng chọn nhanh 1-click
        public List<GoogleAccountItem> SuggestedAccounts { get; set; } = new();

        // Model cho form nhập tài khoản Google khác
        public GoogleLoginInputModel CustomAccount { get; set; } = new();
    }

    public class GoogleAccountItem
    {
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string RoleBadge { get; set; } = "Khách hàng";
    }
}

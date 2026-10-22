namespace WebApplication1.Models.DTOs.VietQr
{
    public class VietQrSettings
    {
        public const string SectionName = "VietQrSettings";

        /// <summary>
        /// Tên đăng nhập Basic Authentication do Merchant (quý khách) quy định và điền vào form VietQR Pro
        /// </summary>
        public string AuthUsername { get; set; } = string.Empty;

        /// <summary>
        /// Mật khẩu Basic Authentication do Merchant (quý khách) quy định và điền vào form VietQR Pro
        /// </summary>
        public string AuthPassword { get; set; } = string.Empty;

        /// <summary>
        /// Khóa bí mật dùng để ký và xác thực JWT Bearer Token (Tối thiểu 256 bits / 32 ký tự)
        /// </summary>
        public string SecretKey { get; set; } = string.Empty;

        /// <summary>
        /// Thời hạn của access token (giây), tài liệu VietQR quy định mặc định 300s (5 phút)
        /// </summary>
        public int TokenExpiresInSeconds { get; set; } = 300;

        /// <summary>
        /// Số tài khoản ngân hàng thụ hưởng liên kết VietQR Pro (để xác thực tài khoản nhận)
        /// </summary>
        public string? BankAccount { get; set; }

        /// <summary>
        /// Mã ngân hàng liên kết (VD: MB, BIDV)
        /// </summary>
        public string? BankCode { get; set; }

        /// <summary>
        /// Cho phép phản hồi thành công (HTTP 200 OK) đối với các giao dịch Test/Mô phỏng từ VietQR Step 2 (ví dụ mã ABC123, ABC124, TEST...) chưa có trong DB để hoàn tất kiểm thử
        /// </summary>
        public bool AllowTestCallbackWithoutOrder { get; set; } = true;
    }
}

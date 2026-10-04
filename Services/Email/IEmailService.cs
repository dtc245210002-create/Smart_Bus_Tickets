namespace WebApplication1.Services.Email
{
    public interface IEmailService
    {
        /// <summary>
        /// Gửi email nội dung HTML tùy chỉnh đến người nhận
        /// </summary>
        Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody);

        /// <summary>
        /// Gửi email chứa mã xác thực OTP 6 chữ số với giao diện chuẩn SmartBus Go
        /// </summary>
        Task<bool> SendOtpEmailAsync(string toEmail, string otpCode, string recipientName = "Quý khách");
    }
}

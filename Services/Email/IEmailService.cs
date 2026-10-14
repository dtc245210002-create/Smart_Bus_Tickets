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

        /// <summary>
        /// Gửi email thông báo và xác nhận vé điện tử (E-Ticket) sau khi thanh toán thành công
        /// </summary>
        Task<bool> SendTicketConfirmationAsync(
            string toEmail, 
            string recipientName, 
            string ticketCode, 
            string bookingCode, 
            string routeName, 
            string seatNumber, 
            decimal amount, 
            string departureTime, 
            string tripDate);
    }
}

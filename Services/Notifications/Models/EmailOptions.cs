namespace WebApplication1.Services.Notifications.Models
{
    public class EmailOptions
    {
        public string SmtpServer { get; set; } = "smtp.gmail.com";
        public int SmtpPort { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string SenderEmail { get; set; } = "tickets@smartbus.vn";
        public string SenderName { get; set; } = "SmartBus Go Ticketing";
        public string? Username { get; set; }
        public string? Password { get; set; }
        public bool SimulationMode { get; set; } = true; // Mặc định chế độ giả lập lưu file nếu chưa cấu hình mật khẩu SMTP
    }
}

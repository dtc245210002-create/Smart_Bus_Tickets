using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WebApplication1.Services.Email
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            var server = _configuration["SmtpSettings:Server"] ?? "smtp.gmail.com";
            var portStr = _configuration["SmtpSettings:Port"] ?? "587";
            var senderEmail = _configuration["SmtpSettings:SenderEmail"];
            var senderName = _configuration["SmtpSettings:SenderName"] ?? "SmartBus Go - Hệ Thống Vé Xe";
            var username = _configuration["SmtpSettings:Username"];
            var password = _configuration["SmtpSettings:Password"];
            var enableSsl = bool.TryParse(_configuration["SmtpSettings:EnableSsl"], out var ssl) ? ssl : true;

            int.TryParse(portStr, out var port);
            if (port <= 0) port = 587;

            // Nếu chưa cấu hình tài khoản SMTP hợp lệ
            if (string.IsNullOrWhiteSpace(username) || 
                string.IsNullOrWhiteSpace(password) || 
                username.Contains("example.com") || 
                username.Contains("your-email@gmail.com") ||
                password.Contains("your-app-password"))
            {
                _logger.LogWarning("EmailService: SMTP chưa được cấu hình tài khoản thật trong appsettings.json. Bỏ qua gửi email thật tới {Email}.", toEmail);
                return false;
            }

            if (string.IsNullOrWhiteSpace(senderEmail))
            {
                senderEmail = username;
            }

            try
            {
                using var client = new SmtpClient(server, port)
                {
                    Credentials = new NetworkCredential(username.Trim(), password.Trim()),
                    EnableSsl = enableSsl,
                    Timeout = 12000 // 12 giây
                };

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail.Trim(), senderName),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail.Trim());

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("EmailService: Đã gửi email xác thực thành công tới {Email}", toEmail);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EmailService: Lỗi xảy ra khi gửi email tới {Email}: {Message}", toEmail, ex.Message);
                return false;
            }
        }

        public async Task<bool> SendOtpEmailAsync(string toEmail, string otpCode, string recipientName = "Quý khách")
        {
            var subject = $"[{otpCode}] Mã xác thực tài khoản SmartBus Go của bạn";
            var htmlBody = $@"
<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width""device-width, initial-scale=1.0"">
    <title>Mã Xác Thực SmartBus Go</title>
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
            background-color: #f8fafc;
            margin: 0;
            padding: 24px;
            color: #334155;
        }}
        .email-container {{
            max-width: 560px;
            margin: 0 auto;
            background-color: #ffffff;
            border-radius: 16px;
            overflow: hidden;
            box-shadow: 0 4px 18px rgba(0,0,0,0.06);
            border: 1px solid #e2e8f0;
        }}
        .header {{
            background: linear-gradient(135deg, #0f766e 0%, #0d9488 100%);
            padding: 32px 24px;
            text-align: center;
            color: #ffffff;
        }}
        .header h1 {{
            margin: 0;
            font-size: 24px;
            font-weight: 700;
            letter-spacing: 0.5px;
        }}
        .header p {{
            margin: 6px 0 0;
            font-size: 13px;
            opacity: 0.9;
        }}
        .content {{
            padding: 32px 28px;
        }}
        .greeting {{
            font-size: 16px;
            font-weight: 600;
            color: #0f172a;
            margin-bottom: 12px;
        }}
        .desc {{
            font-size: 14px;
            line-height: 1.6;
            color: #475569;
            margin-bottom: 24px;
        }}
        .otp-box {{
            background: #f0fdfa;
            border: 2px dashed #0d9488;
            border-radius: 12px;
            text-align: center;
            padding: 20px;
            margin: 20px 0 28px;
        }}
        .otp-code {{
            font-size: 38px;
            font-weight: 800;
            letter-spacing: 10px;
            color: #0f766e;
            font-family: 'Courier New', Courier, monospace;
            margin: 0;
        }}
        .otp-note {{
            font-size: 12px;
            color: #0d9488;
            margin-top: 8px;
            font-weight: 600;
        }}
        .warning-box {{
            background-color: #fffbeb;
            border-left: 4px solid #f59e0b;
            padding: 12px 16px;
            border-radius: 4px;
            font-size: 13px;
            color: #92400e;
            line-height: 1.5;
            margin-bottom: 20px;
        }}
        .footer {{
            background-color: #f1f5f9;
            padding: 20px 24px;
            text-align: center;
            font-size: 12px;
            color: #64748b;
            border-top: 1px solid #e2e8f0;
        }}
        .footer a {{
            color: #0d9488;
            text-decoration: none;
            font-weight: 600;
        }}
    </style>
</head>
<body>
    <div class=""email-container"">
        <div class=""header"">
            <h1>SmartBus Go</h1>
            <p>Hệ thống Quản lý & Bán vé xe buýt thông minh toàn quốc</p>
        </div>
        <div class=""content"">
            <div class=""greeting"">Xin chào {recipientName},</div>
            <p class=""desc"">
                Cảm ơn bạn đã lựa chọn sử dụng nền tảng <strong>SmartBus Go</strong>. Để hoàn tất quy trình kích hoạt tài khoản hoặc xác thực bảo mật, vui lòng sử dụng mã OTP dưới đây:
            </p>

            <div class=""otp-box"">
                <div class=""otp-code"">{otpCode}</div>
                <div class=""otp-note"">Mã có hiệu lực trong 10 phút</div>
            </div>

            <div class=""warning-box"">
                <strong>Lưu ý bảo mật:</strong> Tuyệt đối không chia sẻ mã này cho bất kỳ ai, bao gồm cả nhân viên nhà xe hay tổng đài viên hỗ trợ.
            </div>

            <p class=""desc"" style=""font-size: 13px; color: #94a3b8; margin-bottom: 0;"">
                Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email hoặc liên hệ với bộ phận CSKH của chúng tôi để được trợ giúp.
            </p>
        </div>
        <div class=""footer"">
            <p style=""margin: 0 0 6px;"">Tổng đài hỗ trợ 24/7: <strong>1900 6868</strong> | Email: <strong>hotro@smartbusgo.vn</strong></p>
            <p style=""margin: 0;"">&copy; 2026 SmartBus Go. Bảo lưu mọi quyền.</p>
        </div>
    </div>
</body>
</html>";

            return await SendEmailAsync(toEmail, subject, htmlBody);
        }
    }
}

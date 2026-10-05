using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using WebApplication1.Services.Notifications.Models;

namespace WebApplication1.Services.Notifications
{
    public class EmailService : IEmailService
    {
        private readonly EmailOptions _options;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            IOptions<EmailOptions> options, 
            IWebHostEnvironment env,
            ILogger<EmailService> logger)
        {
            _options = options.Value;
            _env = env;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(
            string toEmail,
            string toName,
            string subject,
            string htmlBody,
            IEnumerable<EmailAttachment>? attachments = null)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                _logger.LogWarning("Không thể gửi email vì địa chỉ người nhận trống.");
                return false;
            }

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_options.SenderName, _options.SenderEmail));
                message.To.Add(new MailboxAddress(toName ?? toEmail, toEmail));
                message.Subject = subject;

                var builder = new BodyBuilder
                {
                    HtmlBody = htmlBody
                };

                if (attachments != null)
                {
                    foreach (var att in attachments)
                    {
                        if (att.Data != null && att.Data.Length > 0)
                        {
                            builder.Attachments.Add(att.FileName, att.Data, ContentType.Parse(att.ContentType));
                        }
                    }
                }

                message.Body = builder.ToMessageBody();

                // Nếu bật chế độ Giả lập (SimulationMode) hoặc chưa điền Mật khẩu SMTP thực tế
                if (_options.SimulationMode || string.IsNullOrWhiteSpace(_options.Password))
                {
                    _logger.LogInformation("Đang chạy chế độ Giả lập Email (Simulation Mode). Lưu email ra file để test...");
                    await SaveEmailLocallyAsync(message, toEmail, subject);
                    return true;
                }

                // Gửi qua SMTP thực tế
                using var client = new SmtpClient();
                await client.ConnectAsync(_options.SmtpServer, _options.SmtpPort, _options.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto);
                
                if (!string.IsNullOrWhiteSpace(_options.Username) && !string.IsNullOrWhiteSpace(_options.Password))
                {
                    await client.AuthenticateAsync(_options.Username, _options.Password);
                }

                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Gửi email thành công tới {Email} với tiêu đề '{Subject}'.", toEmail, subject);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra khi gửi email tới {Email}", toEmail);
                return false;
            }
        }

        private async Task SaveEmailLocallyAsync(MimeMessage message, string toEmail, string subject)
        {
            try
            {
                var emailDir = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "sent_emails");
                if (!Directory.Exists(emailDir))
                {
                    Directory.CreateDirectory(emailDir);
                }

                var safeSubject = string.Concat(subject.Split(Path.GetInvalidFileNameChars())).Replace(" ", "_");
                var fileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{safeSubject}.eml";
                var filePath = Path.Combine(emailDir, fileName);

                using (var stream = File.Create(filePath))
                {
                    await message.WriteToAsync(stream);
                }

                // Đồng thời lưu file HTML xem trực tiếp bằng trình duyệt
                var htmlFilePath = Path.Combine(emailDir, $"{DateTime.Now:yyyyMMdd_HHmmss}_{safeSubject}.html");
                await File.WriteAllTextAsync(htmlFilePath, message.HtmlBody);

                _logger.LogInformation("Đã lưu email xem thử tại: {HtmlPath}", htmlFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể lưu file email giả lập ra đĩa.");
            }
        }
    }
}

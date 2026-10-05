using System;
using System.Text.Json;
using WebApplication1.Services.Notifications.Models;

namespace WebApplication1.Services.Notifications
{
    public class NotificationTemplateService : INotificationTemplateService
    {
        public string RenderTicketEmailHtml(TicketNotificationModel model)
        {
            var ticketUrl = string.IsNullOrWhiteSpace(model.WebTicketUrl) 
                ? $"https://smartbus.vn/Ticket/Detail/{model.TicketCode}" 
                : model.WebTicketUrl;

            return $@"<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Vé điện tử SmartBus Go - {model.TicketCode}</title>
</head>
<body style=""margin: 0; padding: 0; background-color: #f1f5f9; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; color: #1e293b;"">
    <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""background-color: #f1f5f9; padding: 30px 10px;"">
        <tr>
            <td align=""center"">
                <!-- Main Container -->
                <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""max-width: 600px; background-color: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 10px 25px rgba(0,0,0,0.06);"">
                    <!-- Brand Header -->
                    <tr>
                        <td style=""background: linear-gradient(135deg, #0A4D46 0%, #0f766e 100%); padding: 32px 30px; text-align: center;"">
                            <h1 style=""margin: 0; color: #ffffff; font-size: 26px; font-weight: 800; letter-spacing: 1px;"">SMARTBUS <span style=""color: #34d399;"">GO</span></h1>
                            <p style=""margin: 6px 0 0 0; color: #a7f3d0; font-size: 13px; font-weight: 500; text-transform: uppercase; letter-spacing: 1.5px;"">Vé Xe Điện Tử & Xác Nhận Đặt Chỗ</p>
                        </td>
                    </tr>

                    <!-- Status Notification Banner -->
                    <tr>
                        <td style=""background-color: #ecfdf5; border-bottom: 2px solid #a7f3d0; padding: 14px 24px; text-align: center;"">
                            <p style=""margin: 0; color: #065f46; font-size: 14px; font-weight: 600;"">
                                &#10004; Chúc mừng quý khách! Vé xe của bạn đã được thanh toán & giữ chỗ thành công.
                            </p>
                        </td>
                    </tr>

                    <!-- Content Body -->
                    <tr>
                        <td style=""padding: 30px;"">
                            <p style=""margin: 0 0 18px 0; font-size: 15px; line-height: 1.6; color: #334155;"">
                                Kính gửi quý khách <strong>{model.PassengerName}</strong>,<br>
                                Cảm ơn bạn đã lựa chọn <strong>SmartBus Go</strong> cho chuyến đi của mình. Dưới đây là thông tin chi tiết vé điện tử của bạn:
                            </p>

                            <!-- Boarding Pass Card -->
                            <div style=""background-color: #f8fafc; border: 1.5px solid #e2e8f0; border-radius: 12px; padding: 22px; margin-bottom: 25px;"">
                                <table width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"">
                                    <tr>
                                        <td style=""padding-bottom: 12px;"">
                                            <span style=""font-size: 12px; color: #64748b; text-transform: uppercase; font-weight: 600;"">Mã vé</span><br>
                                            <strong style=""font-size: 17px; color: #0A4D46;"">{model.TicketCode}</strong>
                                        </td>
                                        <td align=""right"" style=""padding-bottom: 12px;"">
                                            <span style=""font-size: 12px; color: #64748b; text-transform: uppercase; font-weight: 600;"">Mã đặt chỗ</span><br>
                                            <strong style=""font-size: 15px; color: #475569;"">{model.BookingCode}</strong>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td colspan=""2"" style=""border-top: 1px dashed #cbd5e1; padding-top: 14px; padding-bottom: 14px;"">
                                            <span style=""font-size: 12px; color: #64748b; text-transform: uppercase; font-weight: 600;"">Tuyến đường</span><br>
                                            <strong style=""font-size: 18px; color: #0f172a;"">{model.RouteName}</strong>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style=""padding-bottom: 12px; width: 50%;"">
                                            <span style=""font-size: 12px; color: #64748b; text-transform: uppercase; font-weight: 600;"">Ngày khởi hành</span><br>
                                            <strong style=""font-size: 15px; color: #1e293b;"">{model.DepartureDate}</strong>
                                        </td>
                                        <td align=""right"" style=""padding-bottom: 12px; width: 50%;"">
                                            <span style=""font-size: 12px; color: #64748b; text-transform: uppercase; font-weight: 600;"">Giờ xuất bến</span><br>
                                            <strong style=""font-size: 18px; color: #059669;"">{model.DepartureTime}</strong>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style=""padding-bottom: 12px;"">
                                            <span style=""font-size: 12px; color: #64748b; text-transform: uppercase; font-weight: 600;"">Vị trí ghế / giường</span><br>
                                            <span style=""display: inline-block; background-color: #0A4D46; color: #ffffff; padding: 4px 12px; border-radius: 6px; font-weight: 700; font-size: 15px; margin-top: 3px;"">{model.SeatNumber}</span>
                                        </td>
                                        <td align=""right"" style=""padding-bottom: 12px;"">
                                            <span style=""font-size: 12px; color: #64748b; text-transform: uppercase; font-weight: 600;"">Loại xe & Biển số</span><br>
                                            <strong style=""font-size: 13px; color: #334155;"">{model.BusTypeName} ({model.LicensePlate})</strong>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td colspan=""2"" style=""border-top: 1px solid #e2e8f0; padding-top: 12px;"">
                                            <span style=""font-size: 12px; color: #64748b; text-transform: uppercase; font-weight: 600;"">Điểm đón khách</span><br>
                                            <strong style=""font-size: 14px; color: #1e293b;"">{model.BoardingStopName}</strong>
                                            <div style=""font-size: 12px; color: #64748b; margin-top: 2px;"">{model.BoardingStopAddress}</div>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td colspan=""2"" style=""padding-top: 10px;"">
                                            <span style=""font-size: 12px; color: #64748b; text-transform: uppercase; font-weight: 600;"">Điểm trả khách</span><br>
                                            <strong style=""font-size: 14px; color: #1e293b;"">{model.DropOffStopName}</strong>
                                            <div style=""font-size: 12px; color: #64748b; margin-top: 2px;"">{model.DropOffStopAddress}</div>
                                        </td>
                                    </tr>
                                </table>
                            </div>

                            <!-- QR Code Section -->
                            <div style=""text-align: center; background-color: #ffffff; border: 2px solid #10b981; border-radius: 12px; padding: 20px; margin-bottom: 25px;"">
                                <p style=""margin: 0 0 10px 0; font-size: 13px; font-weight: 700; color: #0A4D46; text-transform: uppercase; letter-spacing: 0.5px;"">MÃ QR CHECK-IN LÊN XE</p>
                                {(string.IsNullOrEmpty(model.QrCodeBase64) 
                                    ? "<p style=\"color: #64748b;\">Mã QR đính kèm trong tệp PDF</p>" 
                                    : $"<img src=\"{model.QrCodeBase64}\" alt=\"Mã QR vé xe {model.TicketCode}\" width=\"160\" height=\"160\" style=\"display: inline-block; border-radius: 8px; border: 1px solid #e2e8f0; padding: 6px; background: #fff;\" />")}
                                <p style=""margin: 10px 0 0 0; font-size: 12px; color: #64748b;"">
                                    Vui lòng xuất trình mã này cho Bác tài / Phụ xe khi lên xe.<br>
                                    <em>(Hệ thống tự động kích hoạt bảo mật chống trùng lặp)</em>
                                </p>
                            </div>

                            <!-- CTA Buttons -->
                            <table width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"" style=""margin-bottom: 20px;"">
                                <tr>
                                    <td align=""center"">
                                        <a href=""{ticketUrl}"" target=""_blank"" style=""display: inline-block; background-color: #0A4D46; color: #ffffff; text-decoration: none; padding: 14px 28px; border-radius: 8px; font-size: 15px; font-weight: 700; box-shadow: 0 4px 12px rgba(10, 77, 70, 0.25);"">
                                            &#128241; Xem Vé Trực Tuyến & Lưu Offline
                                        </a>
                                    </td>
                                </tr>
                            </table>

                            <p style=""margin: 0; font-size: 12px; color: #64748b; line-height: 1.5; text-align: center;"">
                                File PDF vé điện tử chính thức đã được đính kèm trong email này để quý khách dễ dàng in ra hoặc lưu trữ trên thiết bị.
                            </p>
                        </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                        <td style=""background-color: #0f172a; color: #94a3b8; padding: 24px 30px; text-align: center; font-size: 12px; line-height: 1.6;"">
                            <strong style=""color: #ffffff; font-size: 13px;"">SMARTBUS GO - HÀNH TRÌNH THÔNG MINH, TRẢI NGHIỆM VƯỢT TRỘI</strong><br>
                            Tổng đài CSKH & Hỗ trợ khẩn cấp: <span style=""color: #34d399; font-weight: 700;"">1900 6868</span> (24/7)<br>
                            Email: hotro@smartbus.vn | Website: <a href=""https://smartbus.vn"" style=""color: #34d399; text-decoration: none;"">smartbus.vn</a><br>
                            <span style=""display: inline-block; margin-top: 10px; font-size: 11px; color: #64748b;"">© 2026 TTCSK6N3 SmartBus Go Project. All rights reserved.</span>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
        }

        public string RenderTicketSmsText(TicketNotificationModel model)
        {
            var shortRoute = !string.IsNullOrWhiteSpace(model.RouteName)
                ? model.RouteName.Split('(')[0].Trim()
                : $"{model.StartPoint}-{model.EndPoint}";

            var shortUrl = $"smartbus.vn/t/{model.TicketCode}";

            // Tin nhắn viễn thông chuẩn Brandname <= 160 ký tự
            return $"[SmartBus] Ve #{model.TicketCode}: {shortRoute}, {model.DepartureTime} {model.DepartureDate}, Ghe {model.SeatNumber}. Xem QR len xe: {shortUrl}. Hotline 19006868.";
        }

        public string RenderTicketZaloZnsJson(TicketNotificationModel model)
        {
            var ticketUrl = string.IsNullOrWhiteSpace(model.WebTicketUrl) 
                ? $"https://smartbus.vn/t/{model.TicketCode}" 
                : model.WebTicketUrl;

            var payload = new
            {
                template_id = "SMARTBUS_TICKET_CONFIRM_ZNS_01",
                phone = model.PassengerPhone,
                template_data = new
                {
                    customer_name = model.PassengerName,
                    ticket_code = model.TicketCode,
                    booking_code = model.BookingCode,
                    route_name = model.RouteName,
                    departure_time = $"{model.DepartureTime} - {model.DepartureDate}",
                    seat_number = model.SeatNumber,
                    bus_info = $"{model.BusTypeName} ({model.LicensePlate})",
                    boarding_stop = model.BoardingStopName,
                    price = $"{model.Price:N0} đ",
                    ticket_url = ticketUrl
                },
                tracking_id = $"ZNS_{model.TicketCode}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}"
            };

            return JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}

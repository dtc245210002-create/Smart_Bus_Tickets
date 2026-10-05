using System;
using System.IO;
using System.Threading.Tasks;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using WebApplication1.Services.Notifications.Models;

namespace WebApplication1.Services.Pdf
{
    public class TicketPdfService : ITicketPdfService
    {
        public Task<byte[]> GenerateTicketPdfAsync(TicketNotificationModel model, byte[] qrImageBytes)
        {
            return Task.Run(() =>
            {
                using var document = new PdfDocument();
                document.Info.Title = $"SmartBus Go - Vé điện tử {model.TicketCode}";
                document.Info.Author = "SmartBus Go System";
                document.Info.Subject = "E-Ticket Boarding Pass";

                var page = document.AddPage();
                page.Size = PdfSharpCore.PageSize.A4;
                page.Orientation = PdfSharpCore.PageOrientation.Portrait;

                using (var gfx = XGraphics.FromPdfPage(page))
                {
                    // Màu sắc chuẩn Deep Teal & Mint
                    var tealColor = XColor.FromArgb(10, 77, 70); // #0A4D46
                    var lightTeal = XColor.FromArgb(240, 248, 246);
                    var grayBorder = XColor.FromArgb(220, 225, 230);
                    var textDark = XColor.FromArgb(30, 41, 59);
                    var textMuted = XColor.FromArgb(100, 116, 139);

                    // Fonts
                    var titleFont = new XFont("Arial", 18, XFontStyle.Bold);
                    var sectionFont = new XFont("Arial", 13, XFontStyle.Bold);
                    var boldFont = new XFont("Arial", 10, XFontStyle.Bold);
                    var regularFont = new XFont("Arial", 10, XFontStyle.Regular);
                    var smallFont = new XFont("Arial", 8, XFontStyle.Regular);

                    // 1. Header Banner
                    var headerRect = new XRect(30, 30, page.Width - 60, 70);
                    gfx.DrawRectangle(new XSolidBrush(tealColor), headerRect);
                    gfx.DrawString("SMARTBUS GO - VÉ XE ĐIỆN TỬ", titleFont, XBrushes.White, new XRect(50, 42, page.Width - 100, 25), XStringFormats.TopLeft);
                    gfx.DrawString("Hệ thống Đặt & Quản lý Vé Xe Thông Minh Toàn Quốc | Hotline: 1900 6868", regularFont, XBrushes.MintCream, new XRect(50, 70, page.Width - 100, 20), XStringFormats.TopLeft);

                    // 2. Ticket Main Box
                    var bodyRect = new XRect(30, 110, page.Width - 60, 480);
                    gfx.DrawRectangle(new XPen(grayBorder, 1.5), new XSolidBrush(XColor.FromArgb(255, 255, 255)), bodyRect);

                    // Top summary bar in ticket
                    var summaryBar = new XRect(31, 111, page.Width - 62, 45);
                    gfx.DrawRectangle(new XSolidBrush(lightTeal), summaryBar);
                    gfx.DrawString($"MÃ VÉ: {model.TicketCode}", sectionFont, new XSolidBrush(tealColor), new XRect(45, 125, 250, 20), XStringFormats.TopLeft);
                    gfx.DrawString($"MÃ ĐẶT CHỖ: {model.BookingCode}", boldFont, new XSolidBrush(textMuted), new XRect(page.Width - 250, 125, 190, 20), XStringFormats.TopRight);

                    // Divider line
                    gfx.DrawLine(new XPen(grayBorder, 1), 30, 160, page.Width - 30, 160);

                    // 3. Information Columns
                    double leftColX = 50;
                    double rightColX = 320;
                    double currentY = 180;

                    // Row: Passenger & Route
                    DrawField(gfx, "HÀNH KHÁCH", model.PassengerName, boldFont, textDark, leftColX, currentY);
                    DrawField(gfx, "TUYẾN ĐƯỜNG", model.RouteName, boldFont, tealColor, rightColX, currentY);

                    currentY += 45;
                    DrawField(gfx, "SỐ ĐIỆN THOẠI", model.PassengerPhone, regularFont, textDark, leftColX, currentY);
                    DrawField(gfx, "LOẠI XE & BIỂN SỐ", $"{model.BusTypeName} | {model.LicensePlate}", regularFont, textDark, rightColX, currentY);

                    currentY += 45;
                    DrawField(gfx, "NGÀY KHỞI HÀNH", model.DepartureDate, boldFont, textDark, leftColX, currentY);
                    DrawField(gfx, "GIỜ XUẤT BẾN", model.DepartureTime, sectionFont, tealColor, rightColX, currentY);

                    currentY += 45;
                    DrawField(gfx, "SỐ GHẾ / GIƯỜNG", model.SeatNumber, sectionFont, tealColor, leftColX, currentY);
                    DrawField(gfx, "GIÁ VÉ", $"{model.Price:N0} VNĐ (Đã thanh toán)", boldFont, textDark, rightColX, currentY);

                    currentY += 45;
                    DrawField(gfx, "ĐIỂM ĐÓN KHÁCH", $"{model.BoardingStopName}\n{model.BoardingStopAddress}", regularFont, textDark, leftColX, currentY);

                    currentY += 45;
                    DrawField(gfx, "ĐIỂM TRẢ KHÁCH", $"{model.DropOffStopName}\n{model.DropOffStopAddress}", regularFont, textDark, leftColX, currentY);

                    // 4. QR Code Block (Dưới cùng của vé)
                    gfx.DrawLine(new XPen(grayBorder, 1) { DashStyle = XDashStyle.Dash }, 45, 410, page.Width - 45, 410);

                    if (qrImageBytes != null && qrImageBytes.Length > 0)
                    {
                        try
                        {
                            using var imgStream = new MemoryStream(qrImageBytes);
                            var xImage = XImage.FromStream(() => new MemoryStream(qrImageBytes));
                            double qrSize = 130;
                            double qrX = (page.Width - qrSize) / 2;
                            double qrY = 425;
                            gfx.DrawImage(xImage, qrX, qrY, qrSize, qrSize);

                            gfx.DrawString("XUẤT TRÌNH MÃ QR NÀY KHI LÊN XE ĐỂ TÀI XẾ QUÉT CHECK-IN", boldFont, new XSolidBrush(tealColor), new XRect(30, 565, page.Width - 60, 20), XStringFormats.TopCenter);
                        }
                        catch
                        {
                            gfx.DrawString($"MÃ QR VÉ: {model.TicketCode}", boldFont, new XSolidBrush(tealColor), new XRect(30, 480, page.Width - 60, 20), XStringFormats.TopCenter);
                        }
                    }

                    // 5. Terms & Instructions
                    double noteY = 605;
                    gfx.DrawString("HƯỚNG DẪN DÀNH CHO HÀNH KHÁCH:", boldFont, new XSolidBrush(textDark), new XRect(30, noteY, page.Width - 60, 15), XStringFormats.TopLeft);
                    noteY += 18;
                    gfx.DrawString("1. Vui lòng có mặt tại điểm đón trước giờ khởi hành ít nhất 15-30 phút.", regularFont, new XSolidBrush(textMuted), new XRect(30, noteY, page.Width - 60, 15), XStringFormats.TopLeft);
                    noteY += 16;
                    gfx.DrawString("2. Xuất trình mã QR trên vé này (bản in hoặc màn hình điện thoại) cho nhân viên soát vé.", regularFont, new XSolidBrush(textMuted), new XRect(30, noteY, page.Width - 60, 15), XStringFormats.TopLeft);
                    noteY += 16;
                    gfx.DrawString("3. Hỗ trợ khẩn cấp, đổi/hủy vé: Truy cập website SmartBus Go hoặc gọi tổng đài 1900 6868.", regularFont, new XSolidBrush(textMuted), new XRect(30, noteY, page.Width - 60, 15), XStringFormats.TopLeft);

                    // 6. Footer
                    gfx.DrawLine(new XPen(grayBorder, 1), 30, 770, page.Width - 30, 770);
                    gfx.DrawString($"Vé điện tử được khởi tạo tự động bởi SmartBus Go | Thời gian: {DateTime.Now:dd/MM/yyyy HH:mm:ss}", smallFont, new XSolidBrush(textMuted), new XRect(30, 780, page.Width - 60, 15), XStringFormats.TopCenter);
                }

                using var outputStream = new MemoryStream();
                document.Save(outputStream, false);
                return outputStream.ToArray();
            });
        }

        private static void DrawField(XGraphics gfx, string label, string value, XFont valueFont, XColor valueColor, double x, double y)
        {
            var labelFont = new XFont("Arial", 8, XFontStyle.Bold);
            var labelColor = XColor.FromArgb(140, 150, 160);

            gfx.DrawString(label, labelFont, new XSolidBrush(labelColor), new XRect(x, y, 250, 12), XStringFormats.TopLeft);
            gfx.DrawString(value ?? "", valueFont, new XSolidBrush(valueColor), new XRect(x, y + 14, 250, 25), XStringFormats.TopLeft);
        }
    }
}

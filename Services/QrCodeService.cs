using System;
using QRCoder;

namespace WebApplication1.Services;

public static class QrCodeService
{
    /// <summary>
    /// Tạo mã QR dạng Data URI Base64 PNG hoàn toàn offline, 0ms latency, không phụ thuộc vào Internet
    /// </summary>
    public static string GeneratePngBase64(string payload, byte[]? darkColor = null, byte[]? lightColor = null)
    {
        if (string.IsNullOrWhiteSpace(payload)) return string.Empty;
        try
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
            var qrCode = new PngByteQRCode(qrCodeData);
            
            // Mặc định màu thương hiệu SmartBus #0A4D46 (R:10, G:77, B:70, A:255)
            var dark = darkColor ?? new byte[] { 10, 77, 70, 255 };
            var light = lightColor ?? new byte[] { 255, 255, 255, 255 };

            var qrBytes = qrCode.GetGraphic(12, dark, light);
            return $"data:image/png;base64,{Convert.ToBase64String(qrBytes)}";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[QrCodeService.GeneratePngBase64] Error: {ex.Message}");
            return string.Empty;
        }
    }
}

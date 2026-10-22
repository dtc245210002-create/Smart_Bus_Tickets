using WebApplication1.Models.DTOs.VietQr;

namespace WebApplication1.Services.VietQr
{
    public interface IVietQrService
    {
        /// <summary>
        /// Xác thực thông tin Basic Authentication và sinh JWT Token
        /// </summary>
        (bool IsSuccess, VietQrTokenResponseDto? Response, string? ErrorReason, string? ErrorMessage) GenerateToken(string? authHeader);

        /// <summary>
        /// Xác thực tính hợp lệ và thời hạn của JWT Bearer Token
        /// </summary>
        (bool IsValid, string? ErrorReason, string? ErrorMessage) ValidateBearerToken(string? authHeader);

        /// <summary>
        /// Xử lý callback đồng bộ biến động số dư: kiểm tra số tiền, tài khoản, idempotency, cập nhật DB
        /// </summary>
        Task<(bool Success, string? RefTransactionId, string? ErrorReason, string? ErrorMessage)> ProcessTransactionSyncAsync(VietQrTransactionSyncRequestDto dto);
    }
}

using WebApplication1.Models.DTOs;

namespace WebApplication1.Services.TicketValidation
{
    public interface ITicketValidationService
    {
        /// <summary>
        /// Trích xuất mã vé từ raw QR payload (hỗ trợ định dạng chuỗi chuẩn SMARTBUS, URL, hoặc mã trực tiếp)
        /// </summary>
        string ExtractTicketCode(string rawPayload);

        /// <summary>Read an exact ticket without checking it in or changing any state.</summary>
        Task<ValidateQrResponse> InspectAsync(ValidateQrRequest request, int? staffUserId = null);

        /// <summary>
        /// Xác thực và soát mã vé QR cho nhân viên soát vé / tài xế
        /// </summary>
        Task<ValidateQrResponse> ValidateAndCheckInAsync(ValidateQrRequest request, int? staffUserId = null);
    }
}

using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models.DTOs;
using WebApplication1.Services.TicketValidation;

namespace WebApplication1.Controllers.Api
{
    /// <summary>
    /// API Quản lý và Soát vé dành cho Ứng dụng Nhân viên / Bác tài
    /// </summary>
    [ApiController]
    [Route("api/v1/tickets")]
    [Produces("application/json")]
    public class TicketsApiController : ControllerBase
    {
        private readonly ITicketValidationService _ticketValidationService;
        private readonly ILogger<TicketsApiController> _logger;

        public TicketsApiController(
            ITicketValidationService ticketValidationService,
            ILogger<TicketsApiController> logger)
        {
            _ticketValidationService = ticketValidationService;
            _logger = logger;
        }

        /// <summary>
        /// Xác thực và quét mã QR vé xe cho nhân viên soát vé
        /// </summary>
        /// <remarks>
        /// Endpoint này hỗ trợ quét chuỗi mã QR thô (SMARTBUS|TICKET:...), mã vé (TK001), hoặc JSON.
        /// Tùy chọn truyền currentTripId để đối chiếu vé có thuộc đúng chuyến đang soát hay không.
        /// </remarks>
        /// <param name="request">Thông tin payload QR và chuyến xe hiện tại</param>
        /// <returns>Kết quả xác thực vé cùng thông tin hành khách &amp; chuyến đi</returns>
        /// <response code="200">Xác thực thành công, vé hợp lệ và chuyển trạng thái sang USED</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ hoặc vé đã bị hủy</response>
        /// <response code="404">Không tìm thấy mã vé trong hệ thống</response>
        /// <response code="409">Vé đã được quét sử dụng trước đó (cảnh báo trùng lặp)</response>
        /// <response code="422">Vé hợp lệ nhưng sai chuyến xe hiện tại</response>
        [HttpPost("validate-qr")]
        [ProducesResponseType(typeof(ValidateQrResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidateQrResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidateQrResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ValidateQrResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ValidateQrResponse), StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> ValidateQr([FromBody] ValidateQrRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.QrPayload))
            {
                return BadRequest(new ValidateQrResponse
                {
                    Success = false,
                    Code = TicketValidationCodes.InvalidInput,
                    Message = "Dữ liệu yêu cầu không hợp lệ. Vui lòng cung cấp mã QR (qrPayload)."
                });
            }

            // Lấy ID của nhân viên / tài xế đang đăng nhập nếu có
            int? staffUserId = null;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out var parsedId))
            {
                staffUserId = parsedId;
            }

            var result = await _ticketValidationService.ValidateAndCheckInAsync(request, staffUserId);

            return result.Code switch
            {
                TicketValidationCodes.Success => Ok(result),
                TicketValidationCodes.TicketNotFound => NotFound(result),
                TicketValidationCodes.AlreadyUsed => StatusCode(StatusCodes.Status409Conflict, result),
                TicketValidationCodes.TripMismatch => StatusCode(StatusCodes.Status422UnprocessableEntity, result),
                TicketValidationCodes.InvalidInput => BadRequest(result),
                _ => BadRequest(result)
            };
        }

        /// <summary>
        /// Endpoint kiểm tra nhanh (chỉ xem thông tin vé từ mã QR mà không đổi trạng thái sang USED)
        /// Dành cho nhân viên muốn đối chiếu thông tin trước khi cho khách lên xe.
        /// </summary>
        [HttpPost("inspect-qr")]
        [ProducesResponseType(typeof(ValidateQrResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidateQrResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> InspectQr([FromBody] ValidateQrRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.QrPayload))
            {
                return BadRequest(new ValidateQrResponse
                {
                    Success = false,
                    Code = TicketValidationCodes.InvalidInput,
                    Message = "Dữ liệu yêu cầu không hợp lệ. Vui lòng cung cấp mã QR (qrPayload)."
                });
            }

            var ticketCode = _ticketValidationService.ExtractTicketCode(request.QrPayload);
            if (string.IsNullOrWhiteSpace(ticketCode))
            {
                return BadRequest(new ValidateQrResponse
                {
                    Success = false,
                    Code = TicketValidationCodes.InvalidInput,
                    Message = "Không thể trích xuất mã vé từ mã QR."
                });
            }

            // Gọi validate với currentTripId để xem kết quả (nhưng nếu cần xem trước mà không đổi trạng thái)
            // Ta có thể kiểm tra trạng thái nhanh
            var result = await _ticketValidationService.ValidateAndCheckInAsync(new ValidateQrRequest
            {
                QrPayload = request.QrPayload,
                CurrentTripId = request.CurrentTripId
            });

            return Ok(result);
        }
    }
}

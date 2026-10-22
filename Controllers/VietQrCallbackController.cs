using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models.DTOs.VietQr;
using WebApplication1.Services.VietQr;

namespace WebApplication1.Controllers
{
    [ApiController]
    [IgnoreAntiforgeryToken]
    public class VietQrCallbackController : ControllerBase
    {
        private readonly IVietQrService _vietQrService;
        private readonly ILogger<VietQrCallbackController> _logger;

        public VietQrCallbackController(
            IVietQrService vietQrService,
            ILogger<VietQrCallbackController> logger)
        {
            _vietQrService = vietQrService;
            _logger = logger;
        }

        // =========================================================================
        // API 1: GET TOKEN
        // POST: /api/token_generate (và dự phòng /vqr/api/token_generate)
        // VietQR gọi API này để lấy Bearer token trước khi gửi callback giao dịch
        // =========================================================================
        [HttpPost]
        [Route("api/token_generate")]
        [Route("vqr/api/token_generate")]
        public IActionResult GenerateToken()
        {
            string? authHeader = Request.Headers["Authorization"];

            var (isSuccess, response, errorReason, errorMessage) = _vietQrService.GenerateToken(authHeader);

            if (!isSuccess)
            {
                return Unauthorized(new VietQrTokenErrorResponseDto
                {
                    Status = "FAILED",
                    Message = errorMessage ?? errorReason ?? "Xác thực Basic Auth thất bại."
                });
            }

            return Ok(response);
        }

        // =========================================================================
        // API 2: TRANSACTION SYNC
        // POST: /bank/api/transaction-sync (và dự phòng /vqr/bank/api/transaction-sync)
        // VietQR gọi API này để đồng bộ biến động số dư chuyển khoản sau khi khách thanh toán
        // =========================================================================
        [HttpPost]
        [Route("bank/api/transaction-sync")]
        [Route("vqr/bank/api/transaction-sync")]
        public async Task<IActionResult> TransactionSync([FromBody] VietQrTransactionSyncRequestDto dto)
        {
            // 1. Kiểm tra và xác thực Bearer Token
            string? authHeader = Request.Headers["Authorization"];
            var (isValid, tokenErrorReason, tokenErrorMessage) = _vietQrService.ValidateBearerToken(authHeader);

            if (!isValid)
            {
                return StatusCode(401, new VietQrTransactionSyncErrorResponseDto
                {
                    Error = true,
                    ErrorReason = tokenErrorReason ?? "INVALID_TOKEN",
                    ToastMessage = tokenErrorMessage ?? "Xác thực Bearer token thất bại.",
                    Object = null
                });
            }

            // 2. Xử lý nghiệp vụ đối soát và cập nhật đơn hàng
            var (success, refTxnId, errorReason, errorMessage) = await _vietQrService.ProcessTransactionSyncAsync(dto);

            if (!success)
            {
                return BadRequest(new VietQrTransactionSyncErrorResponseDto
                {
                    Error = true,
                    ErrorReason = errorReason ?? "TRANSACTION_FAILED",
                    ToastMessage = errorMessage ?? "Xử lý giao dịch thất bại.",
                    Object = null
                });
            }

            // 3. Trả về thành công đúng chuẩn VietQR
            return Ok(new VietQrTransactionSyncSuccessResponseDto
            {
                Error = false,
                ErrorReason = null,
                ToastMessage = "Transaction processed successfully",
                Object = new VietQrTransactionResponseObject
                {
                    RefTransactionId = refTxnId ?? "OK"
                }
            });
        }
    }
}

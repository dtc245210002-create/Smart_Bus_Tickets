using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models.DTOs;
using WebApplication1.Services.Voucher;

namespace WebApplication1.Controllers.Api
{
    [ApiController]
    [Route("api/vouchers")]
    [Produces("application/json")]
    public class VouchersApiController : ControllerBase
    {
        private readonly IVoucherService _voucherService;
        private readonly ILogger<VouchersApiController> _logger;

        public VouchersApiController(IVoucherService voucherService, ILogger<VouchersApiController> logger)
        {
            _voucherService = voucherService;
            _logger = logger;
        }

        /// <summary>
        /// Lấy toàn bộ danh sách mã giảm giá / voucher trong hệ thống
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var vouchers = await _voucherService.GetAllVouchersAsync();
            return Ok(new
            {
                success = true,
                count = vouchers.Count,
                data = vouchers
            });
        }

        /// <summary>
        /// Lấy chi tiết một voucher theo mã
        /// </summary>
        [HttpGet("{code}")]
        public async Task<IActionResult> GetByCode(string code)
        {
            var voucher = await _voucherService.GetVoucherAsync(code);
            if (voucher == null)
            {
                return NotFound(new { success = false, message = $"Không tìm thấy voucher '{code}'" });
            }
            return Ok(new { success = true, data = voucher });
        }

        /// <summary>
        /// Tạo mới hoặc cập nhật thông tin voucher (từ Admin Portal)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateOrUpdate([FromBody] VoucherDto voucher)
        {
            if (voucher == null || string.IsNullOrWhiteSpace(voucher.Id))
            {
                return BadRequest(new { success = false, message = "Mã voucher không được để trống." });
            }

            try
            {
                var saved = await _voucherService.CreateOrUpdateVoucherAsync(voucher);
                return Ok(new
                {
                    success = true,
                    message = $"Đã lưu mã voucher '{saved.Id}' thành công!",
                    data = saved
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lưu voucher");
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống khi lưu voucher: " + ex.Message });
            }
        }

        /// <summary>
        /// Xóa một voucher
        /// </summary>
        [HttpDelete("{code}")]
        public async Task<IActionResult> Delete(string code)
        {
            var deleted = await _voucherService.DeleteVoucherAsync(code);
            if (!deleted)
            {
                return NotFound(new { success = false, message = $"Không tìm thấy voucher '{code}' để xóa." });
            }
            return Ok(new { success = true, message = $"Đã xóa voucher '{code}' thành công." });
        }

        /// <summary>
        /// Kiểm tra và áp dụng mã voucher cho đơn đặt vé
        /// </summary>
        [HttpPost("validate")]
        public async Task<IActionResult> Validate([FromBody] ValidateVoucherRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Code))
            {
                return BadRequest(new ValidateVoucherResponse
                {
                    Success = false,
                    Message = "Vui lòng nhập mã giảm giá!"
                });
            }

            var result = await _voucherService.ValidateVoucherAsync(request.Code, request.Amount);
            return Ok(result);
        }

        /// <summary>
        /// Đánh dấu voucher đã được sử dụng (ghi nhận khi thanh toán hoàn tất)
        /// </summary>
        [HttpPost("use")]
        public async Task<IActionResult> Use([FromBody] UseVoucherRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Code))
            {
                return BadRequest(new { success = false, message = "Mã voucher rỗng." });
            }

            var used = await _voucherService.UseVoucherAsync(request.Code);
            return Ok(new { success = used, message = used ? "Đã ghi nhận sử dụng voucher" : "Không tìm thấy voucher" });
        }
    }
}

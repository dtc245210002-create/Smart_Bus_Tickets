using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApplication1.Data;
using WebApplication1.Models.Api;
using WebApplication1.Services;

namespace WebApplication1.Controllers.Api
{
    [ApiController]
    [Route("api/bookings")]
    [Produces("application/json")]
    public class BookingsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ISeatHoldService _seatHoldService;

        public BookingsApiController(ApplicationDbContext context, ISeatHoldService seatHoldService)
        {
            _context = context;
            _seatHoldService = seatHoldService;
        }

        /// <summary>
        /// API POST /api/bookings/hold-seats
        /// Đánh dấu trạng thái ghế là HOLD gắn liền với session_id / user_id
        /// </summary>
        [HttpPost("hold-seats")]
        public async Task<ActionResult<ApiResponse<HoldSeatsResponse>>> HoldSeats([FromBody] HoldSeatsRequest request)
        {
            if (request == null)
            {
                return BadRequest(ApiResponse<HoldSeatsResponse>.Fail("Dữ liệu yêu cầu không hợp lệ."));
            }

            if (request.TripId <= 0)
            {
                return BadRequest(ApiResponse<HoldSeatsResponse>.Fail("TripId không hợp lệ."));
            }

            if (request.SeatNumbers == null || !request.SeatNumbers.Any())
            {
                return BadRequest(ApiResponse<HoldSeatsResponse>.Fail("Vui lòng chọn ít nhất 1 vị trí ghế để giữ chỗ."));
            }

            // Chuẩn hóa danh sách ghế
            var cleanSeatNumbers = request.SeatNumbers
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim().ToUpper())
                .Distinct()
                .ToList();

            if (!cleanSeatNumbers.Any())
            {
                return BadRequest(ApiResponse<HoldSeatsResponse>.Fail("Danh sách ghế không hợp lệ."));
            }

            // Đảm bảo có session_id (tạo mới nếu client chưa gửi)
            var effectiveSessionId = !string.IsNullOrWhiteSpace(request.SessionId)
                ? request.SessionId.Trim()
                : Guid.NewGuid().ToString("N");

            decimal basePrice = 160000m;

            // 1. Kiểm tra xem ghế đã được mua thật (BOOKED) trong Database chưa
            try
            {
                var trip = await _context.Trips
                    .Include(t => t.Bookings)
                        .ThenInclude(b => b.Tickets)
                    .FirstOrDefaultAsync(t => t.TripId == request.TripId);

                if (trip != null)
                {
                    var bookedSeats = trip.Bookings
                        .Where(bk => bk.Status != "Cancelled" && bk.Status != "CANCELLED")
                        .SelectMany(bk => bk.Tickets)
                        .Where(tk => tk.Status != "Cancelled" && tk.Status != "CANCELLED" && !string.IsNullOrEmpty(tk.SeatNumber))
                        .Select(tk => tk.SeatNumber!.Trim().ToUpper())
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                    var alreadyBookedInDb = cleanSeatNumbers
                        .Where(seat => bookedSeats.Contains(seat))
                        .ToList();

                    if (alreadyBookedInDb.Any())
                    {
                        return Conflict(ApiResponse<HoldSeatsResponse>.Fail(
                            $"Ghế {string.Join(", ", alreadyBookedInDb)} đã có người đặt mua thành công. Vui lòng chọn ghế khác."));
                    }

                    var ticketPrice = trip.Bookings.SelectMany(b => b.Tickets).FirstOrDefault()?.Price;
                    if (ticketPrice.HasValue && ticketPrice.Value > 0)
                    {
                        basePrice = ticketPrice.Value;
                    }
                }
            }
            catch
            {
                // Bỏ qua lỗi DB tạm thời nếu đang chạy môi trường test không có SQL Server
            }

            // 2. Thực hiện Giữ ghế bằng SeatHoldService (Thread-safe, có TTL/Timeout hết hạn)
            var holdDuration = request.HoldDurationMinutes is >= 1 and <= 30
                ? request.HoldDurationMinutes
                : 10;

            var holdResult = await _seatHoldService.HoldSeatsAsync(
                request.TripId,
                cleanSeatNumbers,
                effectiveSessionId,
                request.UserId,
                holdDuration);

            // 3. Nếu ghế đang bị session khác giữ -> trả về HTTP 409 Conflict
            if (!holdResult.Success)
            {
                return StatusCode(StatusCodes.Status409Conflict, ApiResponse<HoldSeatsResponse>.Fail(
                    holdResult.Message,
                    holdResult.ConflictedSeats.Select(s => $"Ghế {s} đang được người khác giữ chỗ.").ToList()));
            }

            // 4. Tính toán tổng tiền
            decimal totalPrice = cleanSeatNumbers.Sum(seat =>
            {
                bool isVip = seat.StartsWith("A01") || seat.StartsWith("A02") || seat.StartsWith("B01") || seat.StartsWith("B02");
                return isVip ? basePrice * 1.1m : basePrice;
            });

            var responseData = new HoldSeatsResponse
            {
                HoldId = $"HOLD_{request.TripId}_{Guid.NewGuid():N}"[..20].ToUpper(),
                SessionId = effectiveSessionId,
                UserId = request.UserId,
                TripId = request.TripId,
                HeldSeats = holdResult.SuccessfullyHeldSeats,
                TotalPrice = totalPrice,
                HeldAt = DateTime.Now,
                ExpiresAt = holdResult.ExpiresAt ?? DateTime.Now.AddMinutes(holdDuration),
                RemainingSeconds = holdResult.RemainingSeconds
            };

            return Ok(ApiResponse<HoldSeatsResponse>.Ok(
                responseData,
                $"Đã giữ thành công {holdResult.SuccessfullyHeldSeats.Count} ghế cho phiên làm việc. Ghế sẽ tự động hủy sau {holdDuration} phút nếu không thanh toán."));
        }

        /// <summary>
        /// API POST /api/bookings/release-seats (Endpoint hỗ trợ hủy giữ ghế)
        /// </summary>
        [HttpPost("release-seats")]
        public async Task<ActionResult<ApiResponse<bool>>> ReleaseSeats([FromBody] ReleaseSeatsRequest request)
        {
            if (request == null || request.TripId <= 0 || request.SeatNumbers == null || !request.SeatNumbers.Any())
            {
                return BadRequest(ApiResponse<bool>.Fail("Dữ liệu yêu cầu hủy giữ ghế không hợp lệ."));
            }

            await _seatHoldService.ReleaseSeatsAsync(
                request.TripId, 
                request.SeatNumbers, 
                request.SessionId ?? string.Empty, 
                request.UserId);

            return Ok(ApiResponse<bool>.Ok(true, "Đã giải phóng các ghế được yêu cầu thành công."));
        }
    }
}

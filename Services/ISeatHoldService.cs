using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebApplication1.Services
{
    public class SeatHoldItem
    {
        public int TripId { get; set; }
        public string SeatNumber { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public int? UserId { get; set; }
        public DateTime HeldAtUtc { get; set; }
        public DateTime ExpiresAtUtc { get; set; }

        public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;
        public int RemainingSeconds => Math.Max(0, (int)(ExpiresAtUtc - DateTime.UtcNow).TotalSeconds);
    }

    public class SeatHoldResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> ConflictedSeats { get; set; } = new();
        public List<string> SuccessfullyHeldSeats { get; set; } = new();
        public DateTime? ExpiresAt { get; set; }
        public int RemainingSeconds { get; set; }
    }

    public interface ISeatHoldService
    {
        /// <summary>
        /// Giữ chỗ các ghế được chọn cho session/user với thời gian giới hạn
        /// </summary>
        Task<SeatHoldResult> HoldSeatsAsync(int tripId, IEnumerable<string> seatNumbers, string sessionId, int? userId, int durationMinutes = 10);

        /// <summary>
        /// Hủy giữ ghế chủ động khi khách đổi ý hoặc rời khỏi màn hình đặt vé
        /// </summary>
        Task<bool> ReleaseSeatsAsync(int tripId, IEnumerable<string> seatNumbers, string sessionId, int? userId);

        /// <summary>
        /// Lấy toàn bộ danh sách ghế đang được giữ (chưa hết hạn) của một chuyến xe
        /// </summary>
        Task<IReadOnlyDictionary<string, SeatHoldItem>> GetActiveHoldsForTripAsync(int tripId);

        /// <summary>
        /// Kiểm tra một ghế cụ thể có đang bị giữ không
        /// </summary>
        Task<SeatHoldItem?> GetActiveHoldAsync(int tripId, string seatNumber);
    }
}

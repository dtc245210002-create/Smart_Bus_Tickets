using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication1.Services
{
    public class SeatHoldService : ISeatHoldService
    {
        // Key format: "{tripId}:{seatNumber.ToUpper()}"
        private readonly ConcurrentDictionary<string, SeatHoldItem> _holds = new(StringComparer.OrdinalIgnoreCase);
        private readonly object _lockObject = new();

        public Task<SeatHoldResult> HoldSeatsAsync(
            int tripId, 
            IEnumerable<string> seatNumbers, 
            string sessionId, 
            int? userId, 
            int durationMinutes = 10)
        {
            var cleanSeats = seatNumbers
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim().ToUpper())
                .Distinct()
                .ToList();

            if (!cleanSeats.Any())
            {
                return Task.FromResult(new SeatHoldResult
                {
                    Success = false,
                    Message = "Không có ghế nào được chọn."
                });
            }

            var nowUtc = DateTime.UtcNow;
            var expiresAtUtc = nowUtc.AddMinutes(durationMinutes);
            var result = new SeatHoldResult();

            lock (_lockObject)
            {
                // Bước 1: Dọn dẹp các ghế đã hết hạn để giải phóng tài nguyên
                PurgeExpiredHolds();

                // Bước 2: Kiểm tra xung đột (ghế đã được giữ bởi session/user khác và chưa hết hạn)
                var conflictedSeats = new List<string>();

                foreach (var seat in cleanSeats)
                {
                    var key = GetKey(tripId, seat);
                    if (_holds.TryGetValue(key, out var existingHold))
                    {
                        if (!existingHold.IsExpired)
                        {
                            // Kiểm tra nếu là của chính session/user này thì được phép gia hạn/giữ tiếp
                            bool isSameOwner = (!string.IsNullOrEmpty(sessionId) && string.Equals(existingHold.SessionId, sessionId, StringComparison.OrdinalIgnoreCase))
                                               || (userId.HasValue && existingHold.UserId == userId.Value);

                            if (!isSameOwner)
                            {
                                conflictedSeats.Add(seat);
                            }
                        }
                    }
                }

                if (conflictedSeats.Any())
                {
                    result.Success = false;
                    result.ConflictedSeats = conflictedSeats;
                    result.Message = $"Ghế {string.Join(", ", conflictedSeats)} đang được người khác giữ chỗ. Vui lòng chọn ghế khác.";
                    return Task.FromResult(result);
                }

                // Bước 3: Đánh dấu giữ chỗ thành công cho toàn bộ ghế
                foreach (var seat in cleanSeats)
                {
                    var key = GetKey(tripId, seat);
                    var holdItem = new SeatHoldItem
                    {
                        TripId = tripId,
                        SeatNumber = seat,
                        SessionId = sessionId,
                        UserId = userId,
                        HeldAtUtc = nowUtc,
                        ExpiresAtUtc = expiresAtUtc
                    };

                    _holds[key] = holdItem;
                    result.SuccessfullyHeldSeats.Add(seat);
                }

                result.Success = true;
                result.Message = $"Giữ {result.SuccessfullyHeldSeats.Count} ghế thành công trong {durationMinutes} phút.";
                result.ExpiresAt = expiresAtUtc.ToLocalTime();
                result.RemainingSeconds = (int)(expiresAtUtc - nowUtc).TotalSeconds;
            }

            return Task.FromResult(result);
        }

        public Task<bool> ReleaseSeatsAsync(int tripId, IEnumerable<string> seatNumbers, string sessionId, int? userId)
        {
            var cleanSeats = seatNumbers
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim().ToUpper())
                .ToList();

            lock (_lockObject)
            {
                foreach (var seat in cleanSeats)
                {
                    var key = GetKey(tripId, seat);
                    if (_holds.TryGetValue(key, out var holdItem))
                    {
                        bool isOwner = (!string.IsNullOrEmpty(sessionId) && string.Equals(holdItem.SessionId, sessionId, StringComparison.OrdinalIgnoreCase))
                                       || (userId.HasValue && holdItem.UserId == userId.Value);

                        if (isOwner || holdItem.IsExpired)
                        {
                            _holds.TryRemove(key, out _);
                        }
                    }
                }
            }

            return Task.FromResult(true);
        }

        public Task<IReadOnlyDictionary<string, SeatHoldItem>> GetActiveHoldsForTripAsync(int tripId)
        {
            lock (_lockObject)
            {
                PurgeExpiredHolds();

                var active = _holds.Values
                    .Where(h => h.TripId == tripId && !h.IsExpired)
                    .ToDictionary(h => h.SeatNumber, h => h, StringComparer.OrdinalIgnoreCase);

                return Task.FromResult<IReadOnlyDictionary<string, SeatHoldItem>>(active);
            }
        }

        public Task<SeatHoldItem?> GetActiveHoldAsync(int tripId, string seatNumber)
        {
            var key = GetKey(tripId, seatNumber);
            if (_holds.TryGetValue(key, out var holdItem))
            {
                if (!holdItem.IsExpired)
                {
                    return Task.FromResult<SeatHoldItem?>(holdItem);
                }
                else
                {
                    _holds.TryRemove(key, out _);
                }
            }

            return Task.FromResult<SeatHoldItem?>(null);
        }

        private void PurgeExpiredHolds()
        {
            var now = DateTime.UtcNow;
            var expiredKeys = _holds
                .Where(kvp => kvp.Value.ExpiresAtUtc <= now)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _holds.TryRemove(key, out _);
            }
        }

        private static string GetKey(int tripId, string seatNumber) => $"{tripId}:{seatNumber.Trim().ToUpper()}";
    }
}

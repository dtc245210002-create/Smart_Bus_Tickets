using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models.Api
{
    public class HoldSeatsRequest
    {
        [Required(ErrorMessage = "TripId là bắt buộc.")]
        public int TripId { get; set; }

        [Required(ErrorMessage = "Danh sách ghế không được để trống.")]
        [MinLength(1, ErrorMessage = "Vui lòng chọn ít nhất 1 ghế.")]
        public List<string> SeatNumbers { get; set; } = new();

        /// <summary>
        /// Phiên làm việc của khách hàng (Session ID do client tạo hoặc cookie cấp)
        /// </summary>
        public string? SessionId { get; set; }

        /// <summary>
        /// ID người dùng nếu đã đăng nhập
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// Thời gian giữ ghế tính theo phút (Mặc định 10 phút)
        /// </summary>
        [Range(1, 30, ErrorMessage = "Thời gian giữ ghế từ 1 đến 30 phút.")]
        public int HoldDurationMinutes { get; set; } = 10;
    }

    public class HoldSeatsResponse
    {
        public string HoldId { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public int? UserId { get; set; }
        public int TripId { get; set; }
        public List<string> HeldSeats { get; set; } = new();
        public decimal TotalPrice { get; set; }
        public DateTime HeldAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public int RemainingSeconds { get; set; }
    }

    public class ReleaseSeatsRequest
    {
        [Required(ErrorMessage = "TripId là bắt buộc.")]
        public int TripId { get; set; }

        [Required(ErrorMessage = "Danh sách ghế không được để trống.")]
        public List<string> SeatNumbers { get; set; } = new();

        public string? SessionId { get; set; }
        public int? UserId { get; set; }
    }
}

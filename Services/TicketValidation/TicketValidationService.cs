using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models.DTOs;
using WebApplication1.Models.Entities;

namespace WebApplication1.Services.TicketValidation
{
    public class TicketValidationService : ITicketValidationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TicketValidationService> _logger;

        public TicketValidationService(ApplicationDbContext context, ILogger<TicketValidationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Trích xuất mã vé linh hoạt từ nhiều định dạng:
        /// 1. Chuỗi SMARTBUS: SMARTBUS|TICKET:TK001|BOOKING:...|SEAT:...|DATE:...|HASH:...
        /// 2. JSON: {"ticketCode": "TK001"} hoặc {"TicketCode": "TK001"}
        /// 3. Link URL: https://smartbus.vn/ticket/detail/TK001 hoặc ?id=TK001
        /// 4. Mã vé thô: TK001, SBG-HN-DN-20251024-008
        /// </summary>
        public string ExtractTicketCode(string rawPayload)
        {
            if (string.IsNullOrWhiteSpace(rawPayload))
            {
                return string.Empty;
            }

            var trimmed = rawPayload.Trim();

            // Trường hợp 1: Format chuỗi SMARTBUS
            if (trimmed.Contains("TICKET:", StringComparison.OrdinalIgnoreCase))
            {
                var segments = trimmed.Split(new[] { '|', ';', '&' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var seg in segments)
                {
                    var part = seg.Trim();
                    if (part.StartsWith("TICKET:", StringComparison.OrdinalIgnoreCase))
                    {
                        var code = part.Substring("TICKET:".Length).Trim();
                        if (!string.IsNullOrEmpty(code))
                        {
                            return code;
                        }
                    }
                }
            }

            // Trường hợp 2: Format JSON
            if (trimmed.StartsWith("{") && trimmed.EndsWith("}"))
            {
                try
                {
                    using var doc = JsonDocument.Parse(trimmed);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("ticketCode", out var prop) ||
                        root.TryGetProperty("TicketCode", out prop) ||
                        root.TryGetProperty("code", out prop) ||
                        root.TryGetProperty("id", out prop))
                    {
                        var val = prop.GetString();
                        if (!string.IsNullOrWhiteSpace(val)) return val.Trim();
                    }
                }
                catch
                {
                    // Không phải json hợp lệ, bỏ qua fallback sang phương án tiếp theo
                }
            }

            // Trường hợp 3: Format URL (Ví dụ: /Ticket/Detail/TK001 hoặc ?id=TK001)
            if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Contains("/Ticket/Detail", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
                    {
                        // Kiểm tra query parameter ?id=... hoặc ?ticketCode=...
                        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                        var queryId = query["id"] ?? query["ticketCode"] ?? query["code"];
                        if (!string.IsNullOrWhiteSpace(queryId))
                        {
                            return queryId.Trim();
                        }

                        // Kiểm tra path segments: .../Detail/{code}
                        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                        if (segments.Length > 0)
                        {
                            return segments[^1].Trim();
                        }
                    }
                }
                catch
                {
                    // Fallback
                }
            }

            // Trường hợp 4: Mã vé trực tiếp
            return trimmed;
        }

        public async Task<ValidateQrResponse> ValidateAndCheckInAsync(ValidateQrRequest request, int? staffUserId = null)
        {
            if (string.IsNullOrWhiteSpace(request.QrPayload))
            {
                return new ValidateQrResponse
                {
                    Success = false,
                    Code = TicketValidationCodes.InvalidInput,
                    Message = "Dữ liệu mã QR trống. Vui lòng quét hoặc nhập mã vé."
                };
            }

            var ticketCode = ExtractTicketCode(request.QrPayload);
            if (string.IsNullOrWhiteSpace(ticketCode))
            {
                return new ValidateQrResponse
                {
                    Success = false,
                    Code = TicketValidationCodes.InvalidInput,
                    Message = "Không thể đọc được mã vé từ dữ liệu QR đã quét."
                };
            }

            // Truy vấn thông tin vé chi tiết từ cơ sở dữ liệu
            var ticket = await _context.Tickets
                .Include(t => t.Booking)
                    .ThenInclude(b => b.User)
                .Include(t => t.Booking)
                    .ThenInclude(b => b.Trip)
                        .ThenInclude(tr => tr.Route)
                .Include(t => t.Booking)
                    .ThenInclude(b => b.Trip)
                        .ThenInclude(tr => tr.Bus)
                            .ThenInclude(bus => bus.BusType)
                .Include(t => t.Booking)
                    .ThenInclude(b => b.Trip)
                        .ThenInclude(tr => tr.Driver)
                            .ThenInclude(d => d.User)
                .Include(t => t.BoardingStop)
                .Include(t => t.DropOffStop)
                .FirstOrDefaultAsync(t => t.TicketCode == ticketCode || t.TicketId.ToString() == ticketCode);

            if (ticket == null)
            {
                _logger.LogWarning("Soát vé thất bại: Không tìm thấy vé [{TicketCode}] trong hệ thống", ticketCode);
                return new ValidateQrResponse
                {
                    Success = false,
                    Code = TicketValidationCodes.TicketNotFound,
                    Message = $"Không tìm thấy vé có mã [{ticketCode}] trong hệ thống SmartBus."
                };
            }

            var trip = ticket.Booking?.Trip;
            var route = trip?.Route;
            var bus = trip?.Bus;
            var driver = trip?.Driver;
            var passenger = ticket.Booking?.User;

            var ticketData = MapToValidationData(ticket);

            // Kiểm tra ràng buộc đối soát chuyến xe hiện tại (nếu nhân viên gửi kèm currentTripId)
            if (request.CurrentTripId.HasValue && trip != null && trip.TripId != request.CurrentTripId.Value)
            {
                _logger.LogWarning("Soát vé sai chuyến: Vé [{TicketCode}] thuộc Chuyến {TicketTripId}, nhưng đang quét trên Chuyến {CurrentTripId}",
                    ticket.TicketCode, trip.TripId, request.CurrentTripId.Value);

                return new ValidateQrResponse
                {
                    Success = false,
                    Code = TicketValidationCodes.TripMismatch,
                    Message = $"Vé này thuộc Chuyến xe #{trip.TripId} ({route?.RouteName ?? "N/A"} lúc {trip.DepartureTime:hh\\:mm} ngày {trip.TripDate:dd/MM/yyyy}), không trùng khớp với chuyến xe bạn đang soát!",
                    Data = ticketData
                };
            }

            // Kiểm tra trạng thái hiện tại của vé
            var currentStatus = (ticket.Status ?? "").Trim().ToUpperInvariant();

            // 1. Vé đã sử dụng trước đó (Used / Checked-in)
            if (currentStatus == "USED" || currentStatus == "CHECKED_IN" || currentStatus == "CHECKEDIN")
            {
                _logger.LogWarning("Soát vé trùng lặp: Vé [{TicketCode}] của khách {Customer} đã được quét trước đó",
                    ticket.TicketCode, passenger?.FullName);

                return new ValidateQrResponse
                {
                    Success = false,
                    Code = TicketValidationCodes.AlreadyUsed,
                    Message = $"CẢNH BÁO: Vé [{ticket.TicketCode}] này đã được quét lên xe trước đó! Vui lòng kiểm tra lại với hành khách.",
                    Data = ticketData
                };
            }

            // 2. Vé đã bị hủy hoặc hoàn tiền
            if (currentStatus == "CANCELLED" || currentStatus == "CANCELED" || currentStatus == "REFUNDED")
            {
                _logger.LogWarning("Soát vé bị hủy: Vé [{TicketCode}] có trạng thái {Status}", ticket.TicketCode, currentStatus);

                return new ValidateQrResponse
                {
                    Success = false,
                    Code = TicketValidationCodes.TicketCancelled,
                    Message = $"Vé [{ticket.TicketCode}] đã bị hủy hoặc hoàn tiền (Trạng thái: {ticket.Status}). Không được phép lên xe!",
                    Data = ticketData
                };
            }

            // 3. Trạng thái hợp lệ để lên xe (ACTIVE, CONFIRMED, ISSUED, PAID)
            var validStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "ACTIVE", "CONFIRMED", "ISSUED", "PAID"
            };

            if (validStatuses.Contains(currentStatus))
            {
                // Cập nhật trạng thái vé thành ĐÃ LÊN XE (USED)
                ticket.Status = "USED";
                await _context.SaveChangesAsync();

                _logger.LogInformation("Soát vé thành công: Vé [{TicketCode}] - Ghế {Seat} - Khách: {Customer} (StaffId: {StaffId})",
                    ticket.TicketCode, ticket.SeatNumber, passenger?.FullName, staffUserId);

                ticketData.Status = "USED";
                ticketData.CheckInTime = DateTime.Now;

                return new ValidateQrResponse
                {
                    Success = true,
                    Code = TicketValidationCodes.Success,
                    Message = $"Xác thực thành công! Mời hành khách {passenger?.FullName ?? ""} (Ghế: {ticket.SeatNumber ?? "N/A"}) lên xe.",
                    Data = ticketData
                };
            }

            // 4. Các trạng thái bất thường khác (ví dụ: PENDING_PAYMENT, EXPIRED,...)
            _logger.LogWarning("Soát vé không hợp lệ: Vé [{TicketCode}] có trạng thái không hợp lệ: {Status}", ticket.TicketCode, currentStatus);
            return new ValidateQrResponse
            {
                Success = false,
                Code = TicketValidationCodes.InvalidStatus,
                Message = $"Vé [{ticket.TicketCode}] chưa hợp lệ để lên xe (Trạng thái hiện tại: {ticket.Status ?? "Chưa rõ"}).",
                Data = ticketData
            };
        }

        private static TicketValidationData MapToValidationData(Ticket ticket)
        {
            var trip = ticket.Booking?.Trip;
            var route = trip?.Route;
            var bus = trip?.Bus;
            var driver = trip?.Driver;
            var user = ticket.Booking?.User;

            var depTime = trip?.DepartureTime.ToTimeSpan() ?? TimeSpan.Zero;
            var arrTime = trip?.ArrivalTime?.ToTimeSpan();

            return new TicketValidationData
            {
                TicketId = ticket.TicketId,
                TicketCode = ticket.TicketCode,
                Status = ticket.Status ?? "UNKNOWN",
                SeatNumber = ticket.SeatNumber ?? "N/A",
                Price = ticket.Price,
                BookingId = ticket.BookingId,
                BookingCode = ticket.Booking?.BookingCode ?? "N/A",
                BookingTime = ticket.Booking?.BookingTime ?? DateTime.MinValue,
                Passenger = new PassengerDto
                {
                    UserId = user?.UserId ?? 0,
                    FullName = user?.FullName ?? "Hành khách vãng lai",
                    Phone = user?.Phone,
                    Email = user?.Email
                },
                Trip = new TripDto
                {
                    TripId = trip?.TripId ?? 0,
                    RouteCode = route?.RouteCode ?? "N/A",
                    RouteName = route?.RouteName ?? "N/A",
                    StartPoint = route?.StartPoint ?? "N/A",
                    EndPoint = route?.EndPoint ?? "N/A",
                    TripDate = trip?.TripDate ?? DateOnly.MinValue,
                    DepartureTime = depTime,
                    ArrivalTime = arrTime,
                    LicensePlate = bus?.LicensePlate ?? "N/A",
                    BusTypeName = bus?.BusType?.TypeName ?? "N/A",
                    DriverName = driver?.User?.FullName ?? "N/A",
                    DriverPhone = driver?.User?.Phone
                },
                BoardingStop = ticket.BoardingStop?.StopName ?? (route?.StartPoint ?? "Bến khởi hành"),
                BoardingStopAddress = ticket.BoardingStop?.Address,
                DropOffStop = ticket.DropOffStop?.StopName ?? (route?.EndPoint ?? "Bến đích"),
                DropOffStopAddress = ticket.DropOffStop?.Address,
                CheckInTime = DateTime.Now
            };
        }
    }
}

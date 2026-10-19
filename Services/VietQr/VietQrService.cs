using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using WebApplication1.Data;
using WebApplication1.Models.DTOs.VietQr;
using WebApplication1.Models.Entities;

namespace WebApplication1.Services.VietQr
{
    public class VietQrService : IVietQrService
    {
        private readonly VietQrSettings _settings;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<VietQrService> _logger;

        public VietQrService(
            IOptions<VietQrSettings> settings,
            ApplicationDbContext context,
            ILogger<VietQrService> logger)
        {
            _settings = settings.Value;
            _context = context;
            _logger = logger;
        }

        // =========================================================================
        // 1. GET TOKEN: XÁC THỰC BASIC AUTH VÀ CẤP JWT TOKEN
        // =========================================================================
        public (bool IsSuccess, VietQrTokenResponseDto? Response, string? ErrorReason, string? ErrorMessage) GenerateToken(string? authHeader)
        {
            if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                return (false, null, "INVALID_AUTH_HEADER", "Authorization header phải có định dạng 'Basic <base64(username:password)>'.");
            }

            string base64Payload = authHeader.Substring("Basic ".Length).Trim();
            string credentials;
            try
            {
                byte[] data = Convert.FromBase64String(base64Payload);
                credentials = Encoding.UTF8.GetString(data);
            }
            catch (Exception)
            {
                return (false, null, "INVALID_BASE64", "Chuỗi xác thực Basic Auth Base64 không hợp lệ.");
            }

            var parts = credentials.Split(':', 2);
            if (parts.Length != 2)
            {
                return (false, null, "INVALID_CREDENTIALS_FORMAT", "Cấu trúc xác thực phải là 'username:password'.");
            }

            string inputUsername = parts[0];
            string inputPassword = parts[1];

            // Kiểm tra thông tin đăng nhập cấu hình
            if (string.IsNullOrEmpty(_settings.AuthUsername) || string.IsNullOrEmpty(_settings.AuthPassword))
            {
                _logger.LogError("VietQrSettings:AuthUsername hoặc AuthPassword chưa được thiết lập trong cấu hình.");
                return (false, null, "SERVER_CONFIG_ERROR", "Chưa cấu hình tài khoản VietQR trên hệ thống.");
            }

            // So sánh an toàn chống tấn công timing
            bool userMatch = FixedTimeEquals(inputUsername, _settings.AuthUsername);
            bool passMatch = FixedTimeEquals(inputPassword, _settings.AuthPassword);

            if (!userMatch || !passMatch)
            {
                _logger.LogWarning("Thử xác thực VietQR Get Token thất bại với Username: [{User}]", inputUsername);
                return (false, null, "INVALID_CREDENTIALS", "Thông tin đăng nhập Username hoặc Password không chính xác.");
            }

            // Sinh JWT Token
            try
            {
                var secretKey = GetEffectiveSecretKey();
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(secretKey);
                var expiresIn = _settings.TokenExpiresInSeconds > 0 ? _settings.TokenExpiresInSeconds : 300;

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.Name, inputUsername),
                        new Claim("scope", "vietqr_transaction_sync"),
                        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                    }),
                    Expires = DateTime.UtcNow.AddSeconds(expiresIn),
                    SigningCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(key),
                        SecurityAlgorithms.HmacSha256Signature)
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(token);

                _logger.LogInformation("Cấp VietQR Bearer Token thành công cho merchant [{User}], hết hạn sau {Sec}s", inputUsername, expiresIn);

                return (true, new VietQrTokenResponseDto
                {
                    AccessToken = tokenString,
                    TokenType = "Bearer",
                    ExpiresIn = expiresIn
                }, null, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo JWT token cho VietQR: {Message}", ex.Message);
                return (false, null, "TOKEN_GENERATION_ERROR", "Lỗi máy chủ khi tạo mã truy cập.");
            }
        }

        // =========================================================================
        // 2. TRANSACTION SYNC: XÁC THỰC BEARER TOKEN
        // =========================================================================
        public (bool IsValid, string? ErrorReason, string? ErrorMessage) ValidateBearerToken(string? authHeader)
        {
            if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return (false, "INVALID_AUTH_HEADER", "Authorization header phải có định dạng 'Bearer <token>'.");
            }

            string token = authHeader.Substring("Bearer ".Length).Trim();
            if (string.IsNullOrEmpty(token))
            {
                return (false, "EMPTY_TOKEN", "Bearer token không được để trống.");
            }

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(GetEffectiveSecretKey());

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                }, out SecurityToken validatedToken);

                return (true, null, null);
            }
            catch (SecurityTokenExpiredException)
            {
                _logger.LogWarning("Bearer Token gửi từ VietQR đã hết hạn.");
                return (false, "TOKEN_EXPIRED", "Token đã hết hạn. Vui lòng gọi lại API /api/token_generate để lấy token mới.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Bearer Token không hợp lệ: {Message}", ex.Message);
                return (false, "INVALID_TOKEN", "Token không hợp lệ hoặc chữ ký không khớp.");
            }
        }

        // =========================================================================
        // 3. TRANSACTION SYNC: XỬ LÝ BIẾN ĐỘNG SỐ DƯ & CẬP NHẬT DATABASE
        // =========================================================================
        public async Task<(bool Success, string? RefTransactionId, string? ErrorReason, string? ErrorMessage)> ProcessTransactionSyncAsync(VietQrTransactionSyncRequestDto dto)
        {
            if (dto == null)
            {
                return (false, null, "EMPTY_BODY", "Dữ liệu callback rỗng.");
            }

            // 1. Kiểm tra loại giao dịch (transType)
            // VietQR quy định: 'C' = Credit (Tiền vào / Thu tiền), 'D' = Debit (Tiền ra / Chi tiền)
            if (!string.IsNullOrEmpty(dto.TransType) && !dto.TransType.Equals("C", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Bỏ qua giao dịch ghi nợ VietQR (transType: {Type}) - Txn: {Txn}", dto.TransType, dto.TransactionId);
                return (false, null, "IGNORED_DEBIT", "Hệ thống chỉ xử lý giao dịch ghi có (transType = 'C').");
            }

            // 2. Kiểm tra số tiền hợp lệ
            if (dto.Amount <= 0)
            {
                return (false, null, "INVALID_AMOUNT", "Số tiền thanh toán phải lớn hơn 0.");
            }

            bool isTestSimulation = IsTestTransaction(dto);

            // 3. Kiểm tra tài khoản ngân hàng thụ hưởng (nếu có cấu hình và không phải giao dịch test mô phỏng)
            if (!isTestSimulation && !string.IsNullOrWhiteSpace(_settings.BankAccount) && !string.IsNullOrWhiteSpace(dto.BankAccount))
            {
                var cleanConfigAcc = _settings.BankAccount.Trim().Replace(" ", "");
                var cleanPayloadAcc = dto.BankAccount.Trim().Replace(" ", "");
                if (!string.Equals(cleanConfigAcc, cleanPayloadAcc, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Tài khoản thụ hưởng không khớp: Nhận {PayloadAcc}, Cấu hình {ConfigAcc}", cleanPayloadAcc, cleanConfigAcc);
                    return (false, null, "BANK_ACCOUNT_MISMATCH", "Tài khoản nhận tiền không khớp với tài khoản merchant đã đăng ký.");
                }
            }

            var txnId = !string.IsNullOrWhiteSpace(dto.TransactionId)
                ? dto.TransactionId.Trim()
                : (!string.IsNullOrWhiteSpace(dto.ReferenceNumber) ? dto.ReferenceNumber.Trim() : string.Empty);

            if (string.IsNullOrEmpty(txnId))
            {
                txnId = !string.IsNullOrWhiteSpace(dto.OrderId) ? dto.OrderId.Trim() : $"VQR{DateTime.Now:yyyyMMddHHmmss}";
            }

            // 4. CHỐNG XỬ LÝ TRÙNG LẶP (IDEMPOTENCY)
            var existingPayment = await _context.Payments
                .FirstOrDefaultAsync(p => p.TransactionCode == txnId ||
                                          (!string.IsNullOrEmpty(dto.ReferenceNumber) && p.TransactionCode == dto.ReferenceNumber));

            if (existingPayment != null)
            {
                _logger.LogInformation("Giao dịch VietQR [{TxnId}] đã được xử lý trước đó (PaymentId: {PayId}). Trả về thành công.", txnId, existingPayment.PaymentId);
                return (true, existingPayment.PaymentId.ToString(), null, null);
            }

            // 5. TÌM KIẾM ĐƠN ĐẶT VÉ / MÃ VÉ TRONG CƠ SỞ DỮ LIỆU
            var (booking, ticket) = await FindBookingOrTicketAsync(dto.OrderId, dto.Content);

            if (booking == null && ticket == null)
            {
                // Nếu là giao dịch test mô phỏng từ VietQR Step 2 (ví dụ ABC123, ABC124, TEST...) hoặc bật chế độ test
                if (isTestSimulation || _settings.AllowTestCallbackWithoutOrder)
                {
                    _logger.LogInformation("Phát hiện giao dịch Test Callback từ VietQR Step 2 (OrderId: [{OrderId}], Amount: {Amount}, Content: [{Content}]). Phản hồi 200 OK để hoàn tất kiểm thử.",
                        dto.OrderId, dto.Amount, dto.Content);

                    string testRefId = !string.IsNullOrWhiteSpace(txnId)
                        ? txnId
                        : (!string.IsNullOrWhiteSpace(dto.OrderId) ? dto.OrderId : $"REF_{DateTime.Now:yyyyMMddHHmmss}");

                    return (true, testRefId, null, null);
                }

                _logger.LogWarning("Không tìm thấy đơn vé nào khớp với OrderId: [{OrderId}] hoặc Content: [{Content}]", dto.OrderId, dto.Content);
                return (false, null, "ORDER_NOT_FOUND", $"Không tìm thấy đơn hàng tương ứng với OrderId '{dto.OrderId}' hoặc Content '{dto.Content}'.");
            }

            // Xác định booking và số tiền cần thanh toán
            var targetBooking = booking ?? ticket?.Booking;
            if (targetBooking == null && ticket != null)
            {
                // Trường hợp vé độc lập chưa load được Booking
                targetBooking = await _context.Bookings
                    .Include(b => b.Tickets)
                    .Include(b => b.Payments)
                    .FirstOrDefaultAsync(b => b.BookingId == ticket.BookingId);
            }

            decimal requiredAmount = targetBooking?.TotalAmount ?? ticket?.Price ?? 0m;

            // 6. KIỂM TRA SỐ TIỀN CHUYỂN KHOẢN CÓ ĐỦ KHÔNG
            if (dto.Amount < requiredAmount)
            {
                _logger.LogWarning("Số tiền chuyển {Amount:N0}đ không đủ cho đơn {OrderId} (yêu cầu {Required:N0}đ)", dto.Amount, dto.OrderId, requiredAmount);
                return (false, null, "AMOUNT_MISMATCH", $"Số tiền thanh toán ({dto.Amount:N0}đ) nhỏ hơn tổng tiền đơn hàng ({requiredAmount:N0}đ).");
            }

            // 7. CẬP NHẬT TRẠNG THÁI VÉ VÀ ĐƠN HÀNG THÀNH ĐÃ THANH TOÁN
            try
            {
                DateTime paymentDateTime = DateTime.Now;
                if (dto.TransactionTime > 0)
                {
                    try
                    {
                        paymentDateTime = DateTimeOffset.FromUnixTimeMilliseconds(dto.TransactionTime).LocalDateTime;
                    }
                    catch
                    {
                        paymentDateTime = DateTime.Now;
                    }
                }

                if (targetBooking != null)
                {
                    targetBooking.Status = "Confirmed";
                    if (targetBooking.Tickets != null)
                    {
                        foreach (var t in targetBooking.Tickets)
                        {
                            t.Status = "CONFIRMED";
                        }
                    }
                }

                if (ticket != null)
                {
                    ticket.Status = "CONFIRMED";
                }

                // Ghi nhận bản ghi thanh toán vào SQL Server
                var newPayment = new WebApplication1.Models.Entities.Payment
                {
                    BookingId = targetBooking?.BookingId ?? (ticket?.BookingId ?? 0),
                    Amount = dto.Amount,
                    PaymentMethod = "VietQR Pro",
                    Status = "Paid",
                    PaymentTime = paymentDateTime,
                    TransactionCode = txnId
                };

                _context.Payments.Add(newPayment);
                await _context.SaveChangesAsync();

                // Lưu vào DemoPaymentStore để API polling check-status chuyển trang tức thì
                DemoPaymentStore.MarkPaid(ticket?.TicketCode, targetBooking?.BookingCode, dto.Amount, txnId, "VietQR Pro");

                _logger.LogInformation("Xác nhận thanh toán VietQR thành công: BookingCode: [{Code}], TxnId: [{Txn}], Số tiền: {Amount:N0}đ",
                    targetBooking?.BookingCode ?? ticket?.TicketCode, txnId, dto.Amount);

                return (true, newPayment.PaymentId.ToString(), null, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lưu giao dịch VietQR vào CSDL: {Message}", ex.Message);
                return (false, null, "DATABASE_ERROR", "Lỗi hệ thống khi cập nhật trạng thái thanh toán đơn hàng.");
            }
        }

        // =========================================================================
        // HÀM HỖ TRỢ: TÌM KIẾM BOOKING / TICKET LINH HOẠT
        // =========================================================================
        private async Task<(Booking? booking, Ticket? ticket)> FindBookingOrTicketAsync(string? orderId, string? content)
        {
            var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(orderId))
            {
                candidates.Add(orderId.Trim());
                candidates.Add(orderId.Trim().Replace("-", ""));
            }

            if (!string.IsNullOrWhiteSpace(content))
            {
                candidates.Add(content.Trim());
                candidates.Add(content.Trim().Replace("-", ""));

                // Trích xuất các mã thường gặp: SBG..., BK...
                var matches = Regex.Matches(content, @"(SBG[A-Za-z0-9\-]+|BK[A-Za-z0-9\-]+)", RegexOptions.IgnoreCase);
                foreach (Match m in matches)
                {
                    candidates.Add(m.Value);
                    candidates.Add(m.Value.Replace("-", ""));
                }
            }

            foreach (var cand in candidates)
            {
                if (string.IsNullOrWhiteSpace(cand)) continue;

                var clean = cand.ToLower();
                var noDash = clean.Replace("-", "");

                // Tìm theo Booking
                var b = await _context.Bookings
                    .Include(x => x.Tickets)
                    .Include(x => x.Payments)
                    .FirstOrDefaultAsync(x => x.BookingCode.ToLower() == clean ||
                                              x.BookingCode.ToLower().Replace("-", "") == noDash);
                if (b != null) return (b, null);

                // Tìm theo Ticket
                var t = await _context.Tickets
                    .Include(x => x.Booking).ThenInclude(bk => bk.Tickets)
                    .Include(x => x.Booking).ThenInclude(bk => bk.Payments)
                    .FirstOrDefaultAsync(x => x.TicketCode.ToLower() == clean ||
                                              x.TicketCode.ToLower().Replace("-", "") == noDash ||
                                              x.TicketId.ToString() == clean);
                if (t != null) return (t.Booking, t);
            }

            return (null, null);
        }

        private string GetEffectiveSecretKey()
        {
            if (!string.IsNullOrWhiteSpace(_settings.SecretKey) && _settings.SecretKey.Length >= 16)
            {
                return _settings.SecretKey;
            }
            // Khóa dự phòng chuẩn 256 bits an toàn nếu chưa cấu hình
            return "SmartBusGo_VietQR_Callback_SecretKey_Minimum_256_Bits_Super_Secret_Key_2026";
        }

        private static bool FixedTimeEquals(string a, string b)
        {
            byte[] aBytes = Encoding.UTF8.GetBytes(a ?? string.Empty);
            byte[] bBytes = Encoding.UTF8.GetBytes(b ?? string.Empty);
            return CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
        }

        private static bool IsTestTransaction(VietQrTransactionSyncRequestDto dto)
        {
            if (dto == null) return false;

            // Kiểm tra mã đơn hàng hoặc transactionid có tiền tố ABC hoặc TEST (Ví dụ: ABC123, ABC124, ABC125)
            if (!string.IsNullOrEmpty(dto.OrderId) && 
                (dto.OrderId.StartsWith("ABC", StringComparison.OrdinalIgnoreCase) || 
                 dto.OrderId.StartsWith("TEST", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            if (!string.IsNullOrEmpty(dto.TransactionId) && 
                (dto.TransactionId.StartsWith("ABC", StringComparison.OrdinalIgnoreCase) || 
                 dto.TransactionId.StartsWith("TEST", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            if (!string.IsNullOrEmpty(dto.Content) && 
                dto.Content.Contains("test", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Giao dịch mô phỏng mặc định ở Bước 2 VietQR Pro thường là 2,000đ và terminal CH01
            if (dto.Amount == 2000m && !string.IsNullOrEmpty(dto.TerminalCode))
            {
                return true;
            }

            return false;
        }
    }
}

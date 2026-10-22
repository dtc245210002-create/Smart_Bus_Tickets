using System.Text.Json.Serialization;

namespace WebApplication1.Models.DTOs.VietQr
{
    /// <summary>
    /// Dữ liệu callback biến động số dư từ VietQR gửi đến API Transaction Sync
    /// </summary>
    public class VietQrTransactionSyncRequestDto
    {
        [JsonPropertyName("bankaccount")]
        public string? BankAccount { get; set; }

        [JsonPropertyName("amount")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public decimal Amount { get; set; }

        /// <summary>
        /// Phân loại giao dịch: "C" (Credit/Ghi có - Tiền vào), "D" (Debit/Ghi nợ - Tiền ra)
        /// </summary>
        [JsonPropertyName("transType")]
        public string? TransType { get; set; }

        /// <summary>
        /// Nội dung chuyển khoản ngân hàng
        /// </summary>
        [JsonPropertyName("content")]
        public string? Content { get; set; }

        /// <summary>
        /// ID định danh duy nhất của giao dịch từ VietQR
        /// </summary>
        [JsonPropertyName("transactionid")]
        public string? TransactionId { get; set; }

        /// <summary>
        /// Thời gian giao dịch (timestamp miliseconds, ví dụ: 1757342061000)
        /// </summary>
        [JsonPropertyName("transactiontime")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public long TransactionTime { get; set; }

        /// <summary>
        /// Mã tham chiếu ngân hàng (FT number / ref number)
        /// </summary>
        [JsonPropertyName("referencenumber")]
        public string? ReferenceNumber { get; set; }

        /// <summary>
        /// Mã đơn hàng / mã đặt chỗ do hệ thống tạo khi làm mã QR
        /// </summary>
        [JsonPropertyName("orderId")]
        public string? OrderId { get; set; }

        [JsonPropertyName("terminalCode")]
        public string? TerminalCode { get; set; }

        [JsonPropertyName("subTerminalCode")]
        public string? SubTerminalCode { get; set; }

        [JsonPropertyName("serviceCode")]
        public string? ServiceCode { get; set; }

        [JsonPropertyName("urlLink")]
        public string? UrlLink { get; set; }

        [JsonPropertyName("sign")]
        public string? Sign { get; set; }
    }

    /// <summary>
    /// Phản hồi thành công HTTP 200 theo quy định VietQR
    /// </summary>
    public class VietQrTransactionSyncSuccessResponseDto
    {
        [JsonPropertyName("error")]
        public bool Error { get; set; } = false;

        [JsonPropertyName("errorReason")]
        public string? ErrorReason { get; set; } = null;

        [JsonPropertyName("toastMessage")]
        public string ToastMessage { get; set; } = "Transaction processed successfully";

        [JsonPropertyName("object")]
        public VietQrTransactionResponseObject Object { get; set; } = new();
    }

    /// <summary>
    /// Đối tượng chứa mã tham chiếu giao dịch nội bộ trả về cho VietQR
    /// </summary>
    public class VietQrTransactionResponseObject
    {
        [JsonPropertyName("reftransactionid")]
        public string RefTransactionId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Phản hồi lỗi HTTP 400 / 401 theo quy định VietQR
    /// </summary>
    public class VietQrTransactionSyncErrorResponseDto
    {
        [JsonPropertyName("error")]
        public bool Error { get; set; } = true;

        [JsonPropertyName("errorReason")]
        public string ErrorReason { get; set; } = string.Empty;

        [JsonPropertyName("toastMessage")]
        public string ToastMessage { get; set; } = string.Empty;

        [JsonPropertyName("object")]
        public object? Object { get; set; } = null;
    }
}

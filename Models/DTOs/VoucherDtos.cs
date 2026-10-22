using System.Text.Json.Serialization;

namespace WebApplication1.Models.DTOs
{
    public class VoucherDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = "amount"; // "amount" hoặc "percent"

        [JsonPropertyName("value")]
        public decimal Value { get; set; }

        [JsonPropertyName("start")]
        public string Start { get; set; } = string.Empty; // yyyy-MM-dd

        [JsonPropertyName("end")]
        public string End { get; set; } = string.Empty; // yyyy-MM-dd

        [JsonPropertyName("minimum")]
        public decimal Minimum { get; set; } = 0;

        [JsonPropertyName("limit")]
        public int Limit { get; set; } = 100;

        [JsonPropertyName("used")]
        public int Used { get; set; } = 0;

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }

    public class ValidateVoucherRequest
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }
    }

    public class ValidateVoucherResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("discountType")]
        public string? DiscountType { get; set; }

        [JsonPropertyName("discountValue")]
        public decimal DiscountValue { get; set; }

        [JsonPropertyName("discountAmount")]
        public decimal DiscountAmount { get; set; }

        [JsonPropertyName("originalTotal")]
        public decimal OriginalTotal { get; set; }

        [JsonPropertyName("newTotal")]
        public decimal NewTotal { get; set; }

        [JsonPropertyName("voucher")]
        public VoucherDto? Voucher { get; set; }
    }

    public class UseVoucherRequest
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }
    }
}

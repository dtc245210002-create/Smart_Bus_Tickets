using System.Text.Json.Serialization;

namespace WebApplication1.Models.DTOs.VietQr
{
    /// <summary>
    /// Response chuẩn HTTP 200 từ API Get Token theo tài liệu VietQR
    /// </summary>
    public class VietQrTokenResponseDto
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = "Bearer";

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; } = 300;
    }

    /// <summary>
    /// Response lỗi HTTP 400/401 khi xác thực Basic Auth thất bại
    /// </summary>
    public class VietQrTokenErrorResponseDto
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = "FAILED";

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }
}

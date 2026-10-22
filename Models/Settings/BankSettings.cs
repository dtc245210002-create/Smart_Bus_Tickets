namespace WebApplication1.Models.Settings
{
    /// <summary>
    /// Cấu hình thông tin tài khoản ngân hàng nhận tiền chuyển khoản qua mã VietQR (Napas 247)
    /// </summary>
    public class BankSettings
    {
        /// <summary>
        /// Mã ngân hàng theo chuẩn VietQR (ví dụ: MB, VCB, TCB, VPB, ICB, BIDV, ACB, TPB, STB, VIB...)
        /// </summary>
        public string BankId { get; set; } = "MB";

        /// <summary>
        /// Tên ngân hàng hiển thị (ví dụ: Ngân hàng TMCP Quân Đội - MB Bank)
        /// </summary>
        public string BankName { get; set; } = "MB Bank (Ngân hàng Quân Đội)";

        /// <summary>
        /// Chi nhánh ngân hàng (tùy chọn)
        /// </summary>
        public string Branch { get; set; } = "Hội sở chính";

        /// <summary>
        /// Số tài khoản ngân hàng nhận tiền thụ hưởng
        /// </summary>
        public string AccountNo { get; set; } = "0978008922";

        /// <summary>
        /// Tên chủ tài khoản thụ hưởng (viết hoa không dấu, ví dụ: NGUYEN VAN TUAN)
        /// </summary>
        public string AccountName { get; set; } = "AU NGUYEN QUANG TUNG";

        /// <summary>
        /// Mẫu giao diện VietQR: compact2 (đẹp nhất, có logo & tên), qr_only, compact, print
        /// </summary>
        public string Template { get; set; } = "compact2";

        /// <summary>
        /// Mã API Token SePAY (nếu sử dụng dịch vụ tự động bắt giao dịch ngân hàng sepay.vn)
        /// </summary>
        public string? SePayApiKey { get; set; }

        /// <summary>
        /// Chữ ký bí mật xác thực Webhook từ SePAY/Casso
        /// </summary>
        public string? WebhookSecret { get; set; }
    }
}

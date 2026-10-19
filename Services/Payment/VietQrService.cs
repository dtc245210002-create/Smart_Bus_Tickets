using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using WebApplication1.Models.Settings;

namespace WebApplication1.Services.Payment
{
    public class VietQrService : IVietQrService
    {
        private BankSettings _settings;
        private readonly IConfiguration _configuration;
        private readonly ILogger<VietQrService> _logger;

        public VietQrService(
            IConfiguration configuration,
            ILogger<VietQrService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _settings = LoadSettings();
        }

        private BankSettings LoadSettings()
        {
            var s = new BankSettings();
            var section = _configuration.GetSection("BankSettings");
            if (section.Exists())
            {
                section.Bind(s);
            }
            return s;
        }

        public BankSettings GetBankSettings()
        {
            return _settings;
        }

        public void UpdateBankSettings(BankSettings newSettings)
        {
            if (newSettings != null)
            {
                _settings = newSettings;
                _logger.LogInformation("Đã cập nhật cấu hình tài khoản ngân hàng VietQR: Ngân hàng [{BankId}] - STK [{AccountNo}] - Chủ TK [{AccountName}]",
                    _settings.BankId, _settings.AccountNo, _settings.AccountName);
            }
        }

        /// <summary>
        /// Tạo URL ảnh mã VietQR (Napas 247) tự động tương thích 100% tất cả App Ngân hàng và Ví điện tử
        /// Cú pháp chuẩn quốc gia: https://img.vietqr.io/image/{bankId}-{accountNo}-{template}.png?amount={amount}&addInfo={memo}&accountName={name}
        /// </summary>
        public string GenerateVietQrUrl(decimal amount, string transferContent)
        {
            var bankId = !string.IsNullOrWhiteSpace(_settings.BankId) ? _settings.BankId.Trim() : "MB";
            var accountNo = !string.IsNullOrWhiteSpace(_settings.AccountNo) ? _settings.AccountNo.Trim() : "0978008922";
            var template = !string.IsNullOrWhiteSpace(_settings.Template) ? _settings.Template.Trim() : "compact2";
            var accountName = !string.IsNullOrWhiteSpace(_settings.AccountName) ? _settings.AccountName.Trim().ToUpperInvariant() : "AU NGUYEN QUANG TUNG";

            long cleanAmount = (long)Math.Max(0, Math.Round(amount, 0));
            var sanitizedContent = SanitizeTransferContent(transferContent);

            var encodedName = Uri.EscapeDataString(accountName);
            var encodedContent = Uri.EscapeDataString(sanitizedContent);

            return $"https://img.vietqr.io/image/{bankId}-{accountNo}-{template}.png?amount={cleanAmount}&addInfo={encodedContent}&accountName={encodedName}";
        }

        /// <summary>
        /// Chuẩn hóa nội dung chuyển khoản không dấu, không ký tự đặc biệt theo quy định của hệ thống ngân hàng
        /// Ví dụ: "SBG-HN-HP-20261018-01" -> "SBG2026101801" hoặc "SBG 84920"
        /// </summary>
        public string SanitizeTransferContent(string rawTicketCode)
        {
            if (string.IsNullOrWhiteSpace(rawTicketCode))
            {
                return $"SBG{DateTime.Now:MMddHHmm}";
            }

            // Loại bỏ dấu tiếng Việt và ký tự đặc biệt
            var content = rawTicketCode.Trim().ToUpperInvariant();
            
            // Xóa dấu gạch ngang nếu cần hoặc giữ định dạng ngắn gọn
            content = Regex.Replace(content, @"[^A-Z0-9 ]", "");

            if (!content.StartsWith("SBG"))
            {
                content = "SBG " + content;
            }

            // Giới hạn độ dài nội dung chuyển khoản tối đa 25 ký tự để tránh bị ngân hàng cắt bớt
            if (content.Length > 25)
            {
                content = content.Substring(0, 25);
            }

            return content;
        }

        /// <summary>
        /// Danh sách 25+ ngân hàng lớn nhất tại Việt Nam được VietQR hỗ trợ quét mã chuyển khoản Napas247
        /// </summary>
        public List<BankInfoItem> GetSupportedBanks()
        {
            return new List<BankInfoItem>
            {
                new BankInfoItem { Code = "MB", Name = "Ngân hàng TMCP Quân Đội", ShortName = "MB Bank", Bin = "970422" },
                new BankInfoItem { Code = "VCB", Name = "Ngân hàng TMCP Ngoại Thương Việt Nam", ShortName = "Vietcombank", Bin = "970436" },
                new BankInfoItem { Code = "TCB", Name = "Ngân hàng TMCP Kỹ Thương Việt Nam", ShortName = "Techcombank", Bin = "970407" },
                new BankInfoItem { Code = "BIDV", Name = "Ngân hàng TMCP Đầu Tư và Phát Triển Việt Nam", ShortName = "BIDV", Bin = "970418" },
                new BankInfoItem { Code = "ICB", Name = "Ngân hàng TMCP Công Thương Việt Nam", ShortName = "VietinBank", Bin = "970415" },
                new BankInfoItem { Code = "VPB", Name = "Ngân hàng TMCP Việt Nam Thịnh Vượng", ShortName = "VPBank", Bin = "970432" },
                new BankInfoItem { Code = "ACB", Name = "Ngân hàng TMCP Á Châu", ShortName = "ACB", Bin = "970416" },
                new BankInfoItem { Code = "TPB", Name = "Ngân hàng TMCP Tiên Phong", ShortName = "TPBank", Bin = "970423" },
                new BankInfoItem { Code = "STB", Name = "Ngân hàng TMCP Sài Gòn Thương Tín", ShortName = "Sacombank", Bin = "970403" },
                new BankInfoItem { Code = "VIB", Name = "Ngân hàng TMCP Quốc Tế Việt Nam", ShortName = "VIB", Bin = "970441" },
                new BankInfoItem { Code = "HDB", Name = "Ngân hàng TMCP Phát Triển TP.HCM", ShortName = "HDBank", Bin = "970437" },
                new BankInfoItem { Code = "SHB", Name = "Ngân hàng TMCP Sài Gòn - Hà Nội", ShortName = "SHB", Bin = "970443" },
                new BankInfoItem { Code = "MSB", Name = "Ngân hàng TMCP Hàng Hải Việt Nam", ShortName = "MSB", Bin = "970426" },
                new BankInfoItem { Code = "OCB", Name = "Ngân hàng TMCP Phương Đông", ShortName = "OCB", Bin = "970448" },
                new BankInfoItem { Code = "LPB", Name = "Ngân hàng TMCP Bưu Điện Liên Việt", ShortName = "LPBank", Bin = "970449" },
                new BankInfoItem { Code = "SEAB", Name = "Ngân hàng TMCP Đông Nam Á", ShortName = "SeABank", Bin = "970440" },
                new BankInfoItem { Code = "VAB", Name = "Ngân hàng TMCP Việt Á", ShortName = "VietABank", Bin = "970427" },
                new BankInfoItem { Code = "BAB", Name = "Ngân hàng TMCP Bắc Á", ShortName = "BacABank", Bin = "970409" },
                new BankInfoItem { Code = "BVB", Name = "Ngân hàng TMCP Bản Việt", ShortName = "BVBank", Bin = "970454" },
                new BankInfoItem { Code = "NAB", Name = "Ngân hàng TMCP Nam Á", ShortName = "NamABank", Bin = "970428" },
                new BankInfoItem { Code = "KLB", Name = "Ngân hàng TMCP Kiên Long", ShortName = "KienlongBank", Bin = "970452" },
                new BankInfoItem { Code = "CAKE", Name = "Ngân hàng số Cake by VPBank", ShortName = "CAKE", Bin = "546034" },
                new BankInfoItem { Code = "TIMO", Name = "Ngân hàng số Timo by BVBank", ShortName = "Timo", Bin = "963388" }
            };
        }
    }
}

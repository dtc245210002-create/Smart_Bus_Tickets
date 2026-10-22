using WebApplication1.Models.Settings;

namespace WebApplication1.Services.Payment
{
    public class BankInfoItem
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ShortName { get; set; } = string.Empty;
        public string Bin { get; set; } = string.Empty;
    }

    public interface IVietQrService
    {
        BankSettings GetBankSettings();
        void UpdateBankSettings(BankSettings newSettings);
        string GenerateVietQrUrl(decimal amount, string transferContent);
        string SanitizeTransferContent(string rawTicketCode);
        List<BankInfoItem> GetSupportedBanks();
    }
}

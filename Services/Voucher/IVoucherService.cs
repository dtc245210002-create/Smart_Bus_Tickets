using System.Collections.Generic;
using System.Threading.Tasks;
using WebApplication1.Models.DTOs;

namespace WebApplication1.Services.Voucher
{
    public interface IVoucherService
    {
        Task<List<VoucherDto>> GetAllVouchersAsync();
        Task<VoucherDto?> GetVoucherAsync(string code);
        Task<VoucherDto> CreateOrUpdateVoucherAsync(VoucherDto voucher);
        Task<bool> DeleteVoucherAsync(string code);
        Task<ValidateVoucherResponse> ValidateVoucherAsync(string code, decimal currentAmount);
        Task<bool> UseVoucherAsync(string code);
    }
}

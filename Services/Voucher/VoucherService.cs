using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using WebApplication1.Models.DTOs;

namespace WebApplication1.Services.Voucher
{
    public class VoucherService : IVoucherService
    {
        private readonly ConcurrentDictionary<string, VoucherDto> _cache = new(StringComparer.OrdinalIgnoreCase);
        private readonly string _storagePath;
        private readonly SemaphoreSlim _fileLock = new(1, 1);
        private readonly ILogger<VoucherService> _logger;

        public VoucherService(IWebHostEnvironment env, ILogger<VoucherService> logger)
        {
            _logger = logger;
            var appDataDir = Path.Combine(env.ContentRootPath, "App_Data");
            if (!Directory.Exists(appDataDir))
            {
                Directory.CreateDirectory(appDataDir);
            }
            _storagePath = Path.Combine(appDataDir, "vouchers.json");

            LoadFromStorage();
        }

        private void LoadFromStorage()
        {
            try
            {
                if (File.Exists(_storagePath))
                {
                    var json = File.ReadAllText(_storagePath);
                    var items = JsonSerializer.Deserialize<List<VoucherDto>>(json);
                    if (items != null && items.Count > 0)
                    {
                        foreach (var v in items)
                        {
                            if (!string.IsNullOrWhiteSpace(v.Id))
                            {
                                v.Id = v.Id.Trim().ToUpperInvariant();
                                _cache[v.Id] = v;
                            }
                        }
                        _logger.LogInformation("Đã tải {Count} voucher từ file lưu trữ {Path}", _cache.Count, _storagePath);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể đọc vouchers.json, sử dụng danh sách mẫu ban đầu.");
            }

            // Seed danh sách voucher mặc định
            SeedDefaultVouchers();
            _ = SaveToStorageAsync();
        }

        private void SeedDefaultVouchers()
        {
            var today = DateTime.Today;
            var defaults = new List<VoucherDto>
            {
                new()
                {
                    Id = "SMARTBUS20K",
                    Type = "amount",
                    Value = 20000,
                    Minimum = 0,
                    Start = today.AddDays(-30).ToString("yyyy-MM-dd"),
                    End = today.AddYears(1).ToString("yyyy-MM-dd"),
                    Limit = 500,
                    Used = 15,
                    Description = "Giảm trực tiếp 20.000 đ cho mọi chuyến xe SmartBus Go"
                },
                new()
                {
                    Id = "SMARTBUSNEW",
                    Type = "amount",
                    Value = 20000,
                    Minimum = 100000,
                    Start = today.AddDays(-30).ToString("yyyy-MM-dd"),
                    End = today.AddYears(1).ToString("yyyy-MM-dd"),
                    Limit = 200,
                    Used = 18,
                    Description = "Ưu đãi khách hàng mới: Giảm 20.000 đ đơn từ 100.000 đ"
                },
                new()
                {
                    Id = "SINHVIEN",
                    Type = "percent",
                    Value = 15,
                    Minimum = 50000,
                    Start = today.AddDays(-30).ToString("yyyy-MM-dd"),
                    End = today.AddYears(1).ToString("yyyy-MM-dd"),
                    Limit = 300,
                    Used = 42,
                    Description = "Ưu đãi Học sinh - Sinh viên giảm 15% giá vé liên tỉnh"
                },
                new()
                {
                    Id = "HSSV",
                    Type = "percent",
                    Value = 15,
                    Minimum = 50000,
                    Start = today.AddDays(-30).ToString("yyyy-MM-dd"),
                    End = today.AddYears(1).ToString("yyyy-MM-dd"),
                    Limit = 300,
                    Used = 25,
                    Description = "Ưu đãi vé tháng và vé chuyến dành cho Học sinh & Sinh viên"
                },
                new()
                {
                    Id = "VIP50K",
                    Type = "amount",
                    Value = 50000,
                    Minimum = 150000,
                    Start = today.AddDays(-30).ToString("yyyy-MM-dd"),
                    End = today.AddYears(1).ToString("yyyy-MM-dd"),
                    Limit = 100,
                    Used = 10,
                    Description = "Giảm 50.000 đ cho khách hàng đặt xe Limousine VIP từ 150.000 đ"
                },
                new()
                {
                    Id = "GIAM30K",
                    Type = "amount",
                    Value = 30000,
                    Minimum = 100000,
                    Start = today.AddDays(-30).ToString("yyyy-MM-dd"),
                    End = today.AddYears(1).ToString("yyyy-MM-dd"),
                    Limit = 150,
                    Used = 8,
                    Description = "Giảm ngay 30.000 đ áp dụng cho tuyến Hà Nội - Thái Nguyên - Cao Bằng"
                },
                new()
                {
                    Id = "HE2026",
                    Type = "amount",
                    Value = 25000,
                    Minimum = 80000,
                    Start = today.AddDays(-30).ToString("yyyy-MM-dd"),
                    End = today.AddYears(1).ToString("yyyy-MM-dd"),
                    Limit = 200,
                    Used = 12,
                    Description = "Chào hè rực rỡ cùng SmartBus Go - Giảm 25.000 đ"
                }
            };

            foreach (var v in defaults)
            {
                _cache[v.Id] = v;
            }
        }

        private async Task SaveToStorageAsync()
        {
            await _fileLock.WaitAsync();
            try
            {
                var list = _cache.Values.OrderBy(v => v.Id).ToList();
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(list, options);
                await File.WriteAllTextAsync(_storagePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi ghi vouchers ra file {Path}", _storagePath);
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public Task<List<VoucherDto>> GetAllVouchersAsync()
        {
            var list = _cache.Values.OrderByDescending(v => v.Id).ToList();
            return Task.FromResult(list);
        }

        public Task<VoucherDto?> GetVoucherAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return Task.FromResult<VoucherDto?>(null);
            _cache.TryGetValue(code.Trim(), out var voucher);
            return Task.FromResult(voucher);
        }

        public async Task<VoucherDto> CreateOrUpdateVoucherAsync(VoucherDto voucher)
        {
            if (voucher == null || string.IsNullOrWhiteSpace(voucher.Id))
            {
                throw new ArgumentException("Mã voucher không được rỗng");
            }

            voucher.Id = voucher.Id.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(voucher.Start))
            {
                voucher.Start = DateTime.Today.ToString("yyyy-MM-dd");
            }
            if (string.IsNullOrWhiteSpace(voucher.End))
            {
                voucher.End = DateTime.Today.AddMonths(3).ToString("yyyy-MM-dd");
            }
            if (voucher.Limit <= 0) voucher.Limit = 100;
            if (voucher.Value < 0) voucher.Value = 0;
            if (voucher.Minimum < 0) voucher.Minimum = 0;

            if (_cache.TryGetValue(voucher.Id, out var existing))
            {
                // Giữ lại số lượt đã dùng nếu không được ghi đè
                if (voucher.Used <= 0 && existing.Used > 0)
                {
                    voucher.Used = existing.Used;
                }
            }

            if (string.IsNullOrWhiteSpace(voucher.Description))
            {
                voucher.Description = string.Equals(voucher.Type, "percent", StringComparison.OrdinalIgnoreCase)
                    ? $"Giảm {voucher.Value}% giá vé"
                    : $"Giảm {voucher.Value:N0} đ";
            }

            _cache[voucher.Id] = voucher;
            await SaveToStorageAsync();
            _logger.LogInformation("Đã lưu voucher: {Id} - {Type} {Value}", voucher.Id, voucher.Type, voucher.Value);
            return voucher;
        }

        public async Task<bool> DeleteVoucherAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return false;
            var clean = code.Trim().ToUpperInvariant();
            var removed = _cache.TryRemove(clean, out _);
            if (removed)
            {
                await SaveToStorageAsync();
            }
            return removed;
        }

        private static bool TryParseVoucherDate(string? dateStr, out DateTime result)
        {
            result = DateTime.MinValue;
            if (string.IsNullOrWhiteSpace(dateStr)) return false;
            string[] formats = { "yyyy-MM-dd", "dd/MM/yyyy", "yyyy/MM/dd", "d/M/yyyy", "dd-MM-yyyy" };
            if (DateTime.TryParseExact(dateStr.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
            {
                return true;
            }
            return DateTime.TryParse(dateStr.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out result)
                || DateTime.TryParse(dateStr.Trim(), out result);
        }

        public Task<ValidateVoucherResponse> ValidateVoucherAsync(string code, decimal currentAmount)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return Task.FromResult(new ValidateVoucherResponse
                {
                    Success = false,
                    Message = "Vui lòng nhập mã giảm giá!",
                    OriginalTotal = currentAmount,
                    NewTotal = currentAmount
                });
            }

            var cleanCode = code.Trim().ToUpperInvariant();
            if (!_cache.TryGetValue(cleanCode, out var voucher))
            {
                return Task.FromResult(new ValidateVoucherResponse
                {
                    Success = false,
                    Message = $"Mã voucher '{cleanCode}' không tồn tại hoặc đã hết hạn sử dụng.",
                    Code = cleanCode,
                    OriginalTotal = currentAmount,
                    NewTotal = currentAmount
                });
            }

            // Kiểm tra ngày bắt đầu / kết thúc
            var today = DateTime.Today;
            if (TryParseVoucherDate(voucher.Start, out var startDate) && today < startDate.Date)
            {
                return Task.FromResult(new ValidateVoucherResponse
                {
                    Success = false,
                    Message = $"Mã '{voucher.Id}' chỉ có hiệu lực từ ngày {startDate:dd/MM/yyyy}.",
                    Code = voucher.Id,
                    OriginalTotal = currentAmount,
                    NewTotal = currentAmount,
                    Voucher = voucher
                });
            }

            if (TryParseVoucherDate(voucher.End, out var endDate) && today > endDate.Date)
            {
                return Task.FromResult(new ValidateVoucherResponse
                {
                    Success = false,
                    Message = $"Mã '{voucher.Id}' đã hết hạn vào ngày {endDate:dd/MM/yyyy}.",
                    Code = voucher.Id,
                    OriginalTotal = currentAmount,
                    NewTotal = currentAmount,
                    Voucher = voucher
                });
            }

            // Kiểm tra số lượt sử dụng
            if (voucher.Limit > 0 && voucher.Used >= voucher.Limit)
            {
                return Task.FromResult(new ValidateVoucherResponse
                {
                    Success = false,
                    Message = $"Mã '{voucher.Id}' đã hết lượt sử dụng ({voucher.Used}/{voucher.Limit} lượt).",
                    Code = voucher.Id,
                    OriginalTotal = currentAmount,
                    NewTotal = currentAmount,
                    Voucher = voucher
                });
            }

            // Kiểm tra đơn hàng tối thiểu
            if (voucher.Minimum > 0 && currentAmount < voucher.Minimum)
            {
                return Task.FromResult(new ValidateVoucherResponse
                {
                    Success = false,
                    Message = $"Đơn hàng tối thiểu {voucher.Minimum:N0} đ để áp dụng mã '{voucher.Id}' (Đơn hiện tại: {currentAmount:N0} đ).",
                    Code = voucher.Id,
                    OriginalTotal = currentAmount,
                    NewTotal = currentAmount,
                    Voucher = voucher
                });
            }

            // Tính số tiền giảm giá
            decimal discount = 0;
            if (string.Equals(voucher.Type, "percent", StringComparison.OrdinalIgnoreCase))
            {
                discount = Math.Round(currentAmount * (voucher.Value / 100m), 0);
            }
            else
            {
                discount = voucher.Value;
            }

            // Không giảm quá số tiền đơn hàng (giữ tối thiểu 0 đ hoặc 10.000 đ)
            if (discount > currentAmount)
            {
                discount = currentAmount;
            }

            var newTotal = Math.Max(0, currentAmount - discount);

            return Task.FromResult(new ValidateVoucherResponse
            {
                Success = true,
                Message = $"Áp dụng thành công! Đã giảm -{discount:N0} đ vào đơn hàng.",
                Code = voucher.Id,
                DiscountType = voucher.Type,
                DiscountValue = voucher.Value,
                DiscountAmount = discount,
                OriginalTotal = currentAmount,
                NewTotal = newTotal,
                Voucher = voucher
            });
        }

        public async Task<bool> UseVoucherAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return false;
            var clean = code.Trim().ToUpperInvariant();
            if (_cache.TryGetValue(clean, out var voucher))
            {
                voucher.Used++;
                await SaveToStorageAsync();
                _logger.LogInformation("Voucher {Id} đã được sử dụng (tổng {Used}/{Limit})", voucher.Id, voucher.Used, voucher.Limit);
                return true;
            }
            return false;
        }
    }
}

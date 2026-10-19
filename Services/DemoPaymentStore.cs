using System;
using System.Collections.Concurrent;

namespace WebApplication1.Services
{
    public class DemoPaymentRecord
    {
        public string TicketCode { get; set; } = string.Empty;
        public string? BookingCode { get; set; }
        public decimal Amount { get; set; }
        public string TransactionCode { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = "VietQR (Demo Sandbox)";
        public DateTime PaidAt { get; set; } = DateTime.Now;
    }

    public static class DemoPaymentStore
    {
        private static readonly ConcurrentDictionary<string, DemoPaymentRecord> _store = new(StringComparer.OrdinalIgnoreCase);

        public static void MarkPaid(string? ticketCode, string? bookingCode, decimal amount, string? transactionCode = null, string? paymentMethod = null)
        {
            var txn = !string.IsNullOrEmpty(transactionCode) 
                ? transactionCode 
                : $"PAYOS{DateTime.Now:yyyyMMddHHmmss}{Random.Shared.Next(100, 999)}";

            var record = new DemoPaymentRecord
            {
                TicketCode = ticketCode ?? string.Empty,
                BookingCode = bookingCode,
                Amount = amount,
                TransactionCode = txn,
                PaymentMethod = paymentMethod ?? "VietQR (Demo Sandbox)",
                PaidAt = DateTime.Now
            };

            if (!string.IsNullOrWhiteSpace(ticketCode))
            {
                var code = ticketCode.Trim();
                _store[code] = record;
                _store[code.Replace("-", "")] = record;
            }

            if (!string.IsNullOrWhiteSpace(bookingCode))
            {
                var bCode = bookingCode.Trim();
                _store[bCode] = record;
                _store[bCode.Replace("-", "")] = record;
            }
        }

        public static bool TryGet(string? code, out DemoPaymentRecord? record)
        {
            record = null;
            if (string.IsNullOrWhiteSpace(code)) return false;

            var clean = code.Trim();
            if (_store.TryGetValue(clean, out record)) return true;

            var noDash = clean.Replace("-", "");
            if (_store.TryGetValue(noDash, out record)) return true;

            return false;
        }

        public static bool IsPaid(string? code)
        {
            return TryGet(code, out _);
        }
    }
}

namespace WebApplication1.Services.Payment;

public sealed class PaymentRequest
{
    public string OrderId { get; init; } = string.Empty;
    public int BookingId { get; init; }
    public decimal Amount { get; init; }
    public string OrderInfo { get; init; } = string.Empty;
    public string ReturnUrl { get; init; } = string.Empty;
    public string NotifyUrl { get; init; } = string.Empty;
    public IDictionary<string, string> ExtraData { get; init; } = new Dictionary<string, string>();
}

public sealed class PaymentResponse
{
    public bool IsSuccess { get; init; }
    public string Provider { get; init; } = string.Empty;
    public string? PaymentUrl { get; init; }
    public string? TransactionCode { get; init; }
    public string? Message { get; init; }
    public string? RawResponse { get; init; }

    public static PaymentResponse Success(
        string provider,
        string? paymentUrl,
        string? transactionCode,
        string? message = null,
        string? rawResponse = null)
        => new()
        {
            IsSuccess = true,
            Provider = provider,
            PaymentUrl = paymentUrl,
            TransactionCode = transactionCode,
            Message = message,
            RawResponse = rawResponse
        };

    public static PaymentResponse Failed(string provider, string message, string? rawResponse = null)
        => new()
        {
            IsSuccess = false,
            Provider = provider,
            Message = message,
            RawResponse = rawResponse
        };
}

public sealed class PaymentCallbackRequest
{
    public IDictionary<string, string> Parameters { get; init; } = new Dictionary<string, string>();
}

public sealed class PaymentCallbackResult
{
    public bool IsValid { get; init; }
    public bool IsSuccessful { get; init; }
    public string Provider { get; init; } = string.Empty;
    public string? TransactionCode { get; init; }
    public string? OrderId { get; init; }
    public decimal? Amount { get; init; }
    public string? Message { get; init; }
}

public sealed class PaymentQueryRequest
{
    public string OrderId { get; init; } = string.Empty;
    public string? TransactionCode { get; init; }
}

public sealed class PaymentQueryResult
{
    public bool IsSuccess { get; init; }
    public bool IsPaid { get; init; }
    public string Provider { get; init; } = string.Empty;
    public string? OrderId { get; init; }
    public string? TransactionCode { get; init; }
    public decimal? Amount { get; init; }
    public string? Status { get; init; }
    public string? Message { get; init; }
    public string? RawResponse { get; init; }
}

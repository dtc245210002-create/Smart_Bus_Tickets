using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace WebApplication1.Services.Payment;

public sealed class MoMoAdapter : PaymentGatewayService
{
    private readonly HttpClient _httpClient;
    private readonly MoMoOptions _options;

    public MoMoAdapter(HttpClient httpClient, IOptions<MoMoOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public string ProviderName => "MoMo";

    public async Task<PaymentResponse> CreatePayment(
        PaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        PaymentGatewayHelpers.ValidateRequest(request);
        EnsureConfigured(_options.CreatePaymentEndpoint, _options.PartnerCode, _options.AccessKey, _options.SecretKey);

        var requestId = Guid.NewGuid().ToString("N");
        var extraData = string.Join("&", request.ExtraData.Select(x => $"{x.Key}={x.Value}"));
        var rawSignature = $"accessKey={_options.AccessKey}&amount={request.Amount:0}&extraData={extraData}&ipnUrl={request.NotifyUrl}&orderId={request.OrderId}&orderInfo={request.OrderInfo}&partnerCode={_options.PartnerCode}&redirectUrl={request.ReturnUrl}&requestId={requestId}&requestType=captureWallet";

        var payload = new
        {
            partnerCode = _options.PartnerCode,
            requestId,
            amount = Convert.ToInt64(request.Amount),
            orderId = request.OrderId,
            orderInfo = request.OrderInfo,
            redirectUrl = request.ReturnUrl,
            ipnUrl = request.NotifyUrl,
            requestType = "captureWallet",
            extraData = extraData,
            lang = "vi",
            signature = PaymentGatewayHelpers.HmacSha256(_options.SecretKey, rawSignature)
        };

        using var response = await _httpClient.PostAsJsonAsync(_options.CreatePaymentEndpoint, payload, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            return PaymentResponse.Failed(ProviderName, $"MoMo HTTP {(int)response.StatusCode}.", raw);

        using var json = JsonDocument.Parse(raw);
        var root = json.RootElement;
        var resultCode = root.TryGetProperty("resultCode", out var resultCodeElement) ? resultCodeElement.GetInt32() : -1;
        var payUrl = root.TryGetProperty("payUrl", out var payUrlElement) ? payUrlElement.GetString() : null;
        var message = root.TryGetProperty("message", out var messageElement) ? messageElement.GetString() : null;

        return resultCode == 0
            ? PaymentResponse.Success(ProviderName, payUrl, request.OrderId, message, raw)
            : PaymentResponse.Failed(ProviderName, message ?? "MoMo rejected the payment request.", raw);
    }

    public PaymentCallbackResult VerifyCallback(PaymentCallbackRequest request)
    {
        var p = request.Parameters;
        var signature = Get(p, "signature");
        var orderId = Get(p, "orderId");
        var requestId = Get(p, "requestId");
        var amount = Get(p, "amount");
        var orderInfo = Get(p, "orderInfo");
        var orderType = Get(p, "orderType");
        var transId = Get(p, "transId");
        var resultCode = Get(p, "resultCode");
        var message = Get(p, "message");
        var partnerCode = Get(p, "partnerCode");
        var responseTime = Get(p, "responseTime");
        var extraData = Get(p, "extraData");

        var rawSignature = $"partnerCode={partnerCode}&orderId={orderId}&requestId={requestId}&amount={amount}&orderInfo={orderInfo}&orderType={orderType}&transId={transId}&resultCode={resultCode}&message={message}&responseTime={responseTime}&extraData={extraData}";
        var expected = PaymentGatewayHelpers.HmacSha256(_options.SecretKey, rawSignature);
        var valid = PaymentGatewayHelpers.FixedTimeEqualsHex(expected, signature ?? string.Empty);

        return new PaymentCallbackResult
        {
            IsValid = valid,
            IsSuccessful = valid && resultCode == "0",
            Provider = ProviderName,
            OrderId = orderId,
            TransactionCode = transId,
            Amount = decimal.TryParse(amount, out var value) ? value : null,
            Message = valid ? message : "Invalid MoMo callback signature."
        };
    }

    public async Task<PaymentQueryResult> QueryTransaction(
        PaymentQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured(_options.QueryTransactionEndpoint, _options.PartnerCode, _options.AccessKey, _options.SecretKey);

        var requestId = Guid.NewGuid().ToString("N");
        var rawSignature = $"accessKey={_options.AccessKey}&orderId={request.OrderId}&partnerCode={_options.PartnerCode}&requestId={requestId}";
        var payload = new
        {
            partnerCode = _options.PartnerCode,
            requestId,
            orderId = request.OrderId,
            signature = PaymentGatewayHelpers.HmacSha256(_options.SecretKey, rawSignature),
            lang = "vi"
        };

        using var response = await _httpClient.PostAsJsonAsync(_options.QueryTransactionEndpoint, payload, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            return FailedQuery($"MoMo HTTP {(int)response.StatusCode}.", raw);

        using var json = JsonDocument.Parse(raw);
        var root = json.RootElement;
        var resultCode = root.TryGetProperty("resultCode", out var code) ? code.GetInt32() : -1;
        var transId = root.TryGetProperty("transId", out var trans) ? trans.ToString() : null;
        var message = root.TryGetProperty("message", out var msg) ? msg.GetString() : null;
        var amount = root.TryGetProperty("amount", out var amountElement) && amountElement.TryGetDecimal(out var amountValue) ? amountValue : (decimal?)null;

        return new PaymentQueryResult
        {
            IsSuccess = true,
            IsPaid = resultCode == 0,
            Provider = ProviderName,
            OrderId = request.OrderId,
            TransactionCode = transId,
            Amount = amount,
            Status = resultCode.ToString(),
            Message = message,
            RawResponse = raw
        };
    }

    private static void EnsureConfigured(params string[] values)
    {
        if (values.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException("MoMo gateway is not configured. Configure MoMo settings before using this adapter.");
    }

    private static string? Get(IDictionary<string, string> parameters, string key)
        => parameters.TryGetValue(key, out var value) ? value : null;

    private PaymentQueryResult FailedQuery(string message, string raw)
        => new() { IsSuccess = false, IsPaid = false, Provider = ProviderName, OrderId = null, Message = message, RawResponse = raw };
}

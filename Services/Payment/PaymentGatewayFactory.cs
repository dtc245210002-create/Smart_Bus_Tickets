namespace WebApplication1.Services.Payment;

public interface IPaymentGatewayFactory
{
    PaymentGatewayService GetGateway(string paymentMethod);
}

public sealed class PaymentGatewayFactory : IPaymentGatewayFactory
{
    private readonly IReadOnlyDictionary<string, PaymentGatewayService> _gateways;

    public PaymentGatewayFactory(IEnumerable<PaymentGatewayService> gateways)
    {
        _gateways = gateways.ToDictionary(
            x => x.ProviderName,
            StringComparer.OrdinalIgnoreCase);
    }

    public PaymentGatewayService GetGateway(string paymentMethod)
    {
        if (string.IsNullOrWhiteSpace(paymentMethod))
            throw new ArgumentException("Payment method is required.", nameof(paymentMethod));

        if (_gateways.TryGetValue(paymentMethod, out var gateway))
            return gateway;

        throw new KeyNotFoundException(
            $"Payment gateway '{paymentMethod}' is not registered.");
    }
}

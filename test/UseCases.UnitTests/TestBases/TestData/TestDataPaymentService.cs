using UseCases.Services.PayOSService;

namespace UseCases.UnitTests.TestBases.TestData;

public class TestDataPaymentService : IPaymentService
{
    public Task<PaymentLinkResult> CreatePaymentLinkAsync(
        Guid bookingId,
        decimal amount,
        string description,
        string buyerName
    )
    {
        return Task.FromResult(
            new PaymentLinkResult(
                PaymentLinkId: "mock-payment-id",
                CheckoutUrl: "http://mock-checkout-url",
                QrCode: "mock-qr-code",
                Status: "pending"
            )
        );
    }
}

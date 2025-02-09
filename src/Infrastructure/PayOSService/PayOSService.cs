using Net.payOS;
using Net.payOS.Types;
using UseCases.DTOs;
using UseCases.Services.PayOSService;

namespace Infrastructure.PayOSService;

public class PayOSService(PayOS payOS, UrlSettings urlSettings) : IPaymentService
{
    public async Task<PaymentLinkResult> CreatePaymentLinkAsync(
        Guid bookingId,
        decimal amount,
        string description,
        string buyerName
    )
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var shortGuid = bookingId.ToString()[..4];
        var orderCode = long.Parse($"{timestamp}{shortGuid}");

        var items = new List<ItemData> { new("Car Rental", 1, (int)amount) };

        var paymentData = new PaymentData(
            orderCode: orderCode,
            amount: (int)amount,
            description: description,
            items: items,
            cancelUrl: urlSettings.CancelUrl,
            returnUrl: urlSettings.ReturnUrl,
            expiredAt: (int?)DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeSeconds(),
            buyerName: buyerName,
            signature: bookingId.ToString()
        );

        var result = await payOS.createPaymentLink(paymentData);

        return new PaymentLinkResult(
            result.paymentLinkId,
            result.checkoutUrl,
            result.qrCode,
            result.status
        );
    }
}

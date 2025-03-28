using Microsoft.Extensions.Caching.Memory;
using Net.payOS;
using Net.payOS.Types;
using UseCases.DTOs;
using UseCases.Services.PayOSService;

namespace Infrastructure.PayOSService;

public class PayOSService(IMemoryCache cache, PayOS payOS, UrlSettings urlSettings)
    : IPaymentService
{
    public async Task<PaymentLinkResult> CreatePaymentLinkAsync(
        Guid bookingId,
        decimal amount,
        string description,
        string buyerName
    )
    {
        // Check if the payment link is already cached
        var cacheKey = $"PaymentLink_{bookingId}";
        if (cache.TryGetValue<PaymentLinkResult>(cacheKey, out var cachedResult))
            return cachedResult!;

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

        var paymentLinkResult = new PaymentLinkResult(
            result.paymentLinkId,
            orderCode,
            result.checkoutUrl,
            result.qrCode,
            result.status
        );

        // Cache the link with a lifetime of 15 minutes
        var cacheEntryOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(
            TimeSpan.FromMinutes(15)
        );

        cache.Set(cacheKey, paymentLinkResult, cacheEntryOptions);

        return paymentLinkResult;
    }
}

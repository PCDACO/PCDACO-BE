namespace UseCases.Services.PayOSService;

public record PaymentLinkResult(
    string PaymentLinkId,
    long OrderCode,
    string CheckoutUrl,
    string QrCode,
    string Status
);

namespace UseCases.Services.PayOSService;

public record PaymentLinkResult(
    string PaymentLinkId,
    string CheckoutUrl,
    string QrCode,
    string Status
);

namespace UseCases.Services.PayOSService;

public interface IPaymentService
{
    Task<PaymentLinkResult> CreatePaymentLinkAsync(
        Guid bookingId,
        decimal amount,
        string description,
        string buyerName
    );
}

namespace UseCases.Services.PaymentTokenService;

public interface IPaymentTokenService
{
    Task<string> GenerateTokenAsync(Guid bookingId);
    Task<Guid?> ValidateTokenAsync(string token);
}

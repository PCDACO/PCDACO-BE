using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;
using UseCases.Services.PaymentTokenService;

namespace Infrastructure.Services;

public class PaymentTokenService(IMemoryCache cache) : IPaymentTokenService
{
    private const int TOKEN_LENGTH = 32;
    private const int TOKEN_EXPIRY_HOURS = 24;

    public async Task<string> GenerateTokenAsync(Guid bookingId)
    {
        // Generate a secure random token
        var tokenBytes = new byte[TOKEN_LENGTH];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(tokenBytes);
        }

        var token = Convert
            .ToBase64String(tokenBytes)
            .Replace("/", "_")
            .Replace("+", "-")
            .Replace("=", "");

        // Store the token with the booking ID
        cache.Set($"payment_token:{token}", bookingId, TimeSpan.FromHours(TOKEN_EXPIRY_HOURS));

        return token;
    }

    public Task<Guid?> ValidateTokenAsync(string token)
    {
        if (cache.TryGetValue($"payment_token:{token}", out Guid bookingId))
        {
            // Remove the token after use (one-time use)
            cache.Remove($"payment_token:{token}");
            return Task.FromResult<Guid?>(bookingId);
        }

        return Task.FromResult<Guid?>(null);
    }
}

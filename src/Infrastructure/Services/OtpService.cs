using Microsoft.Extensions.Caching.Memory;
using UseCases.Abstractions;

namespace Infrastructure.Services;

public class OtpService(IMemoryCache cache) : IOtpService
{
    private readonly IMemoryCache _cache = cache;
    private readonly Random _random = new();
    private const int OTP_LENGTH = 6;
    private readonly TimeSpan _otpExpiry = TimeSpan.FromMinutes(5);

    public string GenerateOtp()
    {
        const string chars = "0123456789";
        return new string(
            [.. Enumerable.Repeat(chars, OTP_LENGTH).Select(s => s[_random.Next(s.Length)])]
        );
    }

    public bool StoreOtp(string email, string otp)
    {
        var cacheKey = $"OTP_{email}";
        _cache.Set(cacheKey, otp, _otpExpiry);
        return true;
    }

    public bool ValidateOtp(string email, string otp)
    {
        var cacheKey = $"OTP_{email}";
        if (_cache.TryGetValue(cacheKey, out string? storedOtp))
        {
            if (storedOtp == otp)
            {
                _cache.Remove(cacheKey);
                return true;
            }
        }
        return false;
    }
}

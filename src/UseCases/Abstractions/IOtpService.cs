namespace UseCases.Abstractions;

public interface IOtpService
{
    string GenerateOtp();
    bool StoreOtp(string email, string otp);
    bool ValidateOtp(string email, string otp);
}

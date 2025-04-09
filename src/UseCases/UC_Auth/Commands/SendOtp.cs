using Ardalis.Result;
using Domain.Shared.EmailTemplates;
using FluentValidation;
using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.Services.EmailService;

namespace UseCases.UC_Auth.Commands;

public class SendOtp
{
    public record Command(string Email, bool? IsResetPassword = false) : IRequest<Result>;

    public class Handler(
        IAppDBContext context,
        IEmailService emailService,
        IOtpService otpService,
        IBackgroundJobClient backgroundJobClient
    ) : IRequestHandler<Command, Result>
    {
        private readonly IAppDBContext _context = context;
        private readonly IEmailService _emailService = emailService;
        private readonly IOtpService _otpService = otpService;
        private readonly IBackgroundJobClient _backgroundJobClient = backgroundJobClient;

        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            // Find user by email
            var user = await _context
                .Users.AsNoTracking()
                .FirstOrDefaultAsync(
                    u => u.Email == request.Email && !u.IsDeleted,
                    cancellationToken
                );

            if (user is null && (bool)request.IsResetPassword!)
                return Result.NotFound("Không tìm thấy người dùng với email này");

            // Generate OTP
            string otp = _otpService.GenerateOtp();

            // Store OTP in memory cache
            _otpService.StoreOtp(request.Email, otp);

            // Enqueue email sending as a background task
            string userName = user?.Name ?? "Người dùng";
            _backgroundJobClient.Enqueue(() => SendOtpEmail(request.Email, otp, userName));

            return Result.SuccessWithMessage("Mã OTP đã được gửi đến email của bạn");
        }

        // Method that will be executed by Hangfire
        public async Task SendOtpEmail(string email, string otp, string userName)
        {
            var emailTemplate = SendOtpTemple(otp, userName);
            await _emailService.SendEmailAsync(email, "Mã xác thực (OTP) của bạn", emailTemplate);
        }
    }

    private static string SendOtpTemple(string otp, string userName)
    {
        return $@"
            <div style='{EmailTemplateStyles.ContainerStyle}'>
                <div style='{EmailTemplateStyles.HeaderStyle(EmailTemplateColors.SuccessHeader)}'>
                    <h2 style='margin: 0;'>Xác thực tài khoản</h2>
                </div>

                <div style='{EmailTemplateStyles.BodyStyle}'>
                    <p>Xin chào {userName},</p>
                    <p>Chúng tôi đã nhận được yêu cầu xác thực tài khoản của bạn.</p>

                    <div style='{EmailTemplateStyles.DetailBoxStyle(EmailTemplateColors.SuccessBackground)}'>
                        <h3 style='color: {EmailTemplateColors.SuccessAccent}; margin-top: 0; text-align: center;'>Mã OTP của bạn</h3>
                        <p style='font-size: 32px; letter-spacing: 5px; text-align: center; font-weight: bold; color: {EmailTemplateColors.SuccessAccent};'>{otp}</p>
                    </div>

                    <div style='background-color: {EmailTemplateColors.Warning}; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <p style='margin: 0;'><strong>Lưu ý:</strong> Mã OTP có hiệu lực trong vòng 5 phút. Vui lòng không chia sẻ mã này với bất kỳ ai.</p>
                    </div>

                    <p style='{EmailTemplateStyles.FooterStyle}'>
                        Cảm ơn bạn đã sử dụng dịch vụ của chúng tôi!<br>
                        <small><strong>Cần hỗ trợ?</strong> Hãy trả lời email này</small>
                    </p>
                </div>
            </div>";
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("Email không được để trống")
                .EmailAddress()
                .WithMessage("Email không hợp lệ");
        }
    }
}

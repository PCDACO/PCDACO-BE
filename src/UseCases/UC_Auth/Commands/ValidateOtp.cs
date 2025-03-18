using Ardalis.Result;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.Utils;

namespace UseCases.UC_Auth.Commands;

public class ValidateOtp
{
    public record Command(string Email, string Otp) : IRequest<Result<Response>>;

    public record Response(string AccessToken, string RefreshToken);

    public class Handler(IAppDBContext context, IOtpService otpService, TokenService tokenService)
        : IRequestHandler<Command, Result<Response>>
    {
        private readonly IAppDBContext _context = context;
        private readonly IOtpService _otpService = otpService;
        private readonly TokenService _tokenService = tokenService;

        public async Task<Result<Response>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            // Find user by email
            var user = await _context.Users.FirstOrDefaultAsync(
                u => u.Email == request.Email && !u.IsDeleted,
                cancellationToken
            );

            if (user is null)
                return Result.NotFound("Không tìm thấy người dùng với email này");

            // Validate the OTP
            bool isValid = _otpService.ValidateOtp(request.Email, request.Otp);
            if (!isValid)
                return Result.Error("Mã OTP không hợp lệ hoặc đã hết hạn");

            // OTP is valid, generate tokens
            string accessToken = _tokenService.GenerateAccessToken(user);
            string refreshToken = _tokenService.GenerateRefreshToken();

            // Invalidate any existing refresh tokens for this user
            await _context
                .RefreshTokens.Where(rt => rt.UserId == user.Id)
                .ExecuteUpdateAsync(
                    rt => rt.SetProperty(u => u.IsUsed, true).SetProperty(u => u.IsRevoked, true),
                    cancellationToken
                );

            // Create new refresh token
            RefreshToken addingRefreshToken = new()
            {
                Token = refreshToken,
                UserId = user.Id,
                ExpiryDate = DateTimeOffset.UtcNow.AddMinutes(60),
            };

            await _context.RefreshTokens.AddAsync(addingRefreshToken, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success(
                new Response(accessToken, refreshToken),
                "Xác thực OTP thành công"
            );
        }
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

            RuleFor(x => x.Otp)
                .NotEmpty()
                .WithMessage("Mã OTP không được để trống")
                .Length(6)
                .WithMessage("Mã OTP phải có 6 ký tự")
                .Matches(@"^\d+$")
                .WithMessage("Mã OTP chỉ chứa số");
        }
    }
}

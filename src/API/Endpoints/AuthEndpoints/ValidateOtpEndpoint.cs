using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using UseCases.UC_Auth.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.AuthEndpoints;

public class ValidateOtpEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/validate-otp", Handle)
            .WithSummary("Validate OTP code")
            .WithDescription(
                "Validates the OTP code sent to user's email and returns authentication tokens"
            )
            .WithTags("Auth");
    }

    private static async Task<IResult> Handle(
        ISender sender,
        ValidateOtpRequest request,
        CancellationToken cancellationToken
    )
    {
        Result<ValidateOtp.Response> result = await sender.Send(
            new ValidateOtp.Command(
                Email: request.Email,
                Otp: request.Otp,
                request.IsResetPassword
            ),
            cancellationToken
        );
        return result.MapResult();
    }

    private sealed record ValidateOtpRequest(
        string Email,
        string Otp,
        bool? IsResetPassword = false
    );
}

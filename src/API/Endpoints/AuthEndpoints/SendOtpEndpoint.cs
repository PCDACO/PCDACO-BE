using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using UseCases.UC_Auth.Commands;

namespace API.Endpoints.AuthEndpoints;

public class SendOtpEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/send-otp", Handle)
            .WithSummary("Send OTP code to user email")
            .WithDescription(
                "Generate and send OTP verification code to the provided email address"
            )
            .WithTags("Auth");
    }

    private static async Task<Microsoft.AspNetCore.Http.IResult> Handle(
        ISender sender,
        SendOtpRequest request,
        CancellationToken cancellationToken
    )
    {
        Result result = await sender.Send(
            new SendOtp.Command(request.Email, request.IsResetPassword),
            cancellationToken
        );

        return result.MapResult();
    }

    private sealed record SendOtpRequest(string Email, bool? IsResetPassword = false);
}

using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using UseCases.UC_Auth.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.AuthEndpoints;

public class ResetPasswordEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/reset-password", Handle)
            .WithSummary("Reset user password")
            .WithDescription("Reset user password with the provided new password")
            .WithTags("Auth")
            .RequireAuthorization();
    }

    private static async Task<IResult> Handle(
        ISender sender,
        ResetPasswordRequest request,
        CancellationToken cancellationToken
    )
    {
        Result result = await sender.Send(
            new ResetPassword.Command(request.NewPassword),
            cancellationToken
        );
        return result.MapResult();
    }

    private record ResetPasswordRequest(string NewPassword);
}

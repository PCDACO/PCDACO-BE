using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using UseCases.UC_User.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.AuthEndpoints;

public sealed class LoginEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", Handle).WithSummary("Login a user").WithTags("Auth");
    }

    private async Task<IResult> Handle(ISender sender, LoginRequest request)
    {
        Result<Login.Response> result = await sender.Send(
            new Login.Command(request.Email, request.Password)
        );
        return result.MapResult();
    }

    private sealed record LoginRequest(string Email, string Password);
}

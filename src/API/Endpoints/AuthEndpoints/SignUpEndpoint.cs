using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using UseCases.UC_User.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.AuthEndpoints;

public class SignUpEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/signup", Handle).WithSummary("Sign up a new user").WithTags("Auth");
    }

    private async Task<IResult> Handle(ISender sender, SignUpRequest request)
    {
        Result<SignUp.Response> result = await sender.Send(
            new SignUp.Command(
                request.Name,
                request.Email,
                request.Password,
                request.Address,
                request.DateOfBirth,
                request.Phone,
                request.RoleName!
            )
        );
        return result.MapResult();
    }

    private record SignUpRequest(
        string Name,
        string Email,
        string Password,
        string Address,
        DateTimeOffset DateOfBirth,
        string Phone,
        string? RoleName = "Driver"
    );
}

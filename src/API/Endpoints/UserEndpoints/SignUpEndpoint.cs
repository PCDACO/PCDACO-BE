using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using UseCases.UC_User.Commands;

namespace API.Endpoints.UserEndpoints;

public class SignUpEndpoint : CarterModule
{
    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/users/signup", Handle)
            .WithSummary("Sign up a new user")
            .WithTags("Users");
    }

    private async Task<Microsoft.AspNetCore.Http.IResult> Handle(ISender sender, SignUpRequest request)
    {
        Result<SignUp.Response> result = await sender.Send(new SignUp.Command(
            request.Name,
            request.Email,
            request.Password,
            request.Address,
            request.DateOfBirth,
            request.Phone
        ));
        return result.MapResult();
    }

    private record SignUpRequest(
        string Name,
        string Email,
        string Password,
        string Address,
        DateTimeOffset DateOfBirth,
        string Phone
    );
}
using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using UseCases.UC_User.Commands;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.UserEndpoints;

public class AdminLoginEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/admin/login", Handle)
            .WithSummary("Admin login")
            .WithTags("Auth");
    }
    private async Task<IResult> Handle(ISender sender, LoginAdminRequest request)
    {
        Result<AdminLogin.Response> result = await sender.Send(new AdminLogin.Command(
            request.Email,
            request.Password
        ));
        return result.MapResult();
    }
    private record LoginAdminRequest(string Email, string Password);
}
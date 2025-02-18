using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using UseCases.UC_User.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.UserEndpoints;

public class ChangePasswordEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPatch("/api/users/{id:guid}/password", Handle)
            .WithSummary("Change user's password")
            .WithTags("Users")
            .RequireAuthorization();
    }

    private async Task<IResult> Handle(ISender sender, Guid id, ChangePasswordRequest request)
    {
        Result<ChangePassword.Response> result = await sender.Send(
            new ChangePassword.Command(id, request.OldPassword, request.NewPassword)
        );
        return result.MapResult();
    }

    private record ChangePasswordRequest(string OldPassword, string NewPassword);
}

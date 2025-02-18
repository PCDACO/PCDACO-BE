using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using UseCases.UC_User.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.UserEndpoints;

public class UpdateUserEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/users/{id:guid}", Handle)
            .WithSummary("Update user profile's information")
            .WithTags("Users")
            .RequireAuthorization();
    }

    private async Task<IResult> Handle(ISender sender, Guid id, UpdateUserRequest request)
    {
        Result<UpdateUser.Response> result = await sender.Send(
            new UpdateUser.Command(
                id,
                request.Name,
                request.Email,
                request.Address,
                request.DateOfBirth,
                request.Phone
            )
        );
        return result.MapResult();
    }

    private record UpdateUserRequest(
        string Name,
        string Email,
        string Address,
        DateTimeOffset DateOfBirth,
        string Phone
    );
}

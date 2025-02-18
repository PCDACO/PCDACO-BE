using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using UseCases.UC_User.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.UserEndpoints;

public class CreateStaffEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/users/staff", Handle)
            .WithSummary("Create a staff user (consultant or technician)")
            .WithTags("Users")
            .RequireAuthorization();
    }

    private async Task<IResult> Handle(ISender sender, CreateStaffRequest request)
    {
        Result<CreateStaff.Response> result = await sender.Send(
            new CreateStaff.Command(
                request.Name,
                request.Email,
                request.Password,
                request.Address,
                request.DateOfBirth,
                request.Phone,
                request.RoleName
            )
        );
        return result.MapResult();
    }

    private record CreateStaffRequest(
        string Name,
        string Email,
        string Password,
        string Address,
        DateTimeOffset DateOfBirth,
        string Phone,
        string RoleName
    );
}

using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using UseCases.UC_User.Queries;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.UserEndpoints;

public class GetUserPendingForApprovalEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/users/{id:guid}/approval", Handle)
            .WithSummary("Get User Need To Be Approve By Id")
            .WithTags("Users")
            .RequireAuthorization();
    }

    public async Task<IResult> Handle(
            Guid id,
            ISender sender
        )
    {
        Result<GetUserPendingForApproval.Response> result =
            await sender.Send(new GetUserPendingForApproval.Query(id));
        return result.MapResult();
    }
}

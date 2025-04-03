using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using UseCases.UC_Activities.Queries;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.ActivityEndpoints;

public class GetTechnicianRecentActivityEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/users/technician/recent-activity", Handle)
            .WithSummary("Get technicians' recent activity")
            .WithTags("Users")
            .RequireAuthorization();
    }

    private async Task<IResult> Handle(
            ISender sender,
            CancellationToken cancellationToken
            )
    {
        Result<GetTechnicianRecentActivity.Response> result = await sender.Send(
                new GetTechnicianRecentActivity.Query(), cancellationToken
                );
        return result.MapResult();
    }
}
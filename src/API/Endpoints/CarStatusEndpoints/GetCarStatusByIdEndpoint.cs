using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using UseCases.UC_CarStatus.Queries;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.CarStatusEndpoints;

public class GetCarStatusByIdEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/car-statuses/{id:guid}", Handle)
            .WithSummary("Get car status by id")
            .WithTags("Car Statuses")
            .RequireAuthorization();
    }
    private async Task<IResult> Handle(ISender sender, Guid id)
    {
        Result<GetCarStatusById.Response> result = await sender.Send(new GetCarStatusById.Query(id));
        return result.MapResult();
    }
}
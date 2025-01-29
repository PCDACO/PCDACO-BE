using API.Utils;

using Ardalis.Result;

using Bogus.DataSets;

using Carter;

using MediatR;

using Microsoft.AspNetCore.Mvc;

using UseCases.UC_CarStatus.Commands;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.CarStatusEndpoints;

public class UpdateCarStatusEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/car-statuses/{id:guid}", Handle)
            .WithSummary("Update a car status")
            .WithTags("Car Statuses")
            .RequireAuthorization();
    }
    private async Task<IResult> Handle(
        ISender sender,
        Guid id,
        [FromBody] UpdateCarStatusRequest request)
    {
        Result result = await sender.Send(new UpdateCarStatus.Command(id, request.Name));
        return result.MapResult();
    }
    private record UpdateCarStatusRequest(string Name);
}
using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using UseCases.UC_CarStatus.Commands;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.CarStatusEndpoints;

public class DeleteCarStatusEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/car-statuses/{id:guid}", Handle)
            .WithSummary("Delete a car status")
            .WithTags("Car Statuses")
            .RequireAuthorization();
    }

    private async Task<IResult> Handle(ISender sender, Guid id)
    {
        Result result = await sender.Send(new DeleteCarStatus.Command(id));
        return result.MapResult();
    }
}
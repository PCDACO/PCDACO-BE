using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using UseCases.UC_CarStatus.Commands;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.CarStatusEndpoints;

public class CreateCarStatusEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/car-statuses", Handle)
            .WithSummary("Create a new car status")
            .WithTags("Car Statuses")
            .RequireAuthorization();
    }
    private async Task<IResult> Handle(ISender sender, CreateCarStatusRequest request)
    {
        Result<CreateCarStatus.Response> result = await sender.Send(new CreateCarStatus.Command(request.Name));
        return result.MapResult();
    }
    private record CreateCarStatusRequest(string Name);
}
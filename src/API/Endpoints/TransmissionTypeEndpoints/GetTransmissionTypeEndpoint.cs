using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using UseCases.UC_TransmissionType.Queries;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.TransmissionTypeEndpoints;

public class GetTransmissionTypeEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/transmission-types/{id:guid}", Handle)
            .WithSummary("Get transmission type by id")
            .WithTags("Transmission Types")
            .RequireAuthorization();
    }
    private async Task<IResult> Handle(ISender sender, Guid id)
    {
        Result<GetTransmissionTypeById.Response> result = await sender.Send(new GetTransmissionTypeById.Query(id));
        return result.MapResult();
    }
}
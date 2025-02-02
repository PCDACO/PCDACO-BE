using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using UseCases.UC_TransmissionType.Commands;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.TransmissionTypeEndpoints;

public class DeleteTransmissionTypeEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/transmission-types/{id:guid}", Handle)
            .WithSummary("Delete a transmission type")
            .WithTags("Transmission Types")
            .RequireAuthorization();
    }
    private async Task<IResult> Handle(
        ISender sender,
        Guid id
    )
    {
        Result result = await sender.Send(new DeleteTransmissionType.Command(id));
        return result.MapResult();
    }
}
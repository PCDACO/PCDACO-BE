using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using UseCases.UC_TransmissionType.Commands;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.TransmissionTypeEndpoints;

public class UpdateTransmissionTypeEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/transmission-types/{id:guid}", Handle)
            .WithSummary("Update a transmission type")
            .WithTags("Transmission Types")
            .RequireAuthorization();
    }
    private async Task<IResult> Handle(ISender sender, Guid id, UpdateTransmissionTypeRequest request)
    {
        Result result = await sender.Send(new UpdateTransmissionType.Command(id, request.Name));
        return result.MapResult();
    }
    private record UpdateTransmissionTypeRequest(string Name);
}
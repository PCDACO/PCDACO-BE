using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using UseCases.UC_TransmissionType.Commands;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.TransmissionTypeEndpoints;

public class CreateTransmissionTypeEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/transmission-types", Handle)
            .WithSummary("Create a new transmission type")
            .WithTags("Transmission Types")
            .RequireAuthorization();
    }
    private async Task<IResult> Handle(
        ISender sender,
        CreateTransmissionTypeRequest request
    )
    {
        Result<CreateTransmissionType.Response> result = await sender.Send(new CreateTransmissionType.Command(request.Name));
        return result.MapResult();
    }
    private record CreateTransmissionTypeRequest(string Name);
}
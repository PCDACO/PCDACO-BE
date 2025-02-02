using API.Utils;

using Ardalis.Result;

using Carter;

using Domain.Entities;

using MediatR;

using Microsoft.AspNetCore.Mvc;

using UseCases.DTOs;
using UseCases.UC_TransmissionType.Queries;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.TransmissionTypeEndpoints;

public class GetTransmissionTypesEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/transmission-types", Handle)
            .WithSummary("Get transmission types")
            .WithTags("Transmission Types")
            .RequireAuthorization();
    }

    private async Task<IResult> Handle(
        ISender sender,
        [FromQuery(Name = "index")] int? pageNumber = 1,
        [FromQuery(Name = "size")] int? pageSize = 10,
        [FromQuery(Name = "keyword")] string? keyword = ""
        )
    {
        Result<OffsetPaginatedResponse<GetTransmissionTypes.Response>> result = 
            await sender.Send(new GetTransmissionTypes.Query(pageNumber!.Value, pageSize!.Value, keyword!));
        return result.MapResult();
    }
}
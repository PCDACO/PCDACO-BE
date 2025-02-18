using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using Microsoft.AspNetCore.Mvc;

using UseCases.DTOs;
using UseCases.UC_GPSDevice.Queries;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.GPSDeviceEndpoints;

public class GetGPSDevicesEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/gps-devices/", Handle)
            .WithTags("GPS Devices")
            .WithSummary("Get GPS Devices")
            .RequireAuthorization();
    }
    private async Task<IResult> Handle(
        ISender sender,
        [FromQuery(Name = "index")] int? pageNumber = 1,
        [FromQuery(Name = "size")] int? pageSize = 10,
        [FromQuery(Name = "keyword")] string? keyword = ""
    )
    {
        Result<OffsetPaginatedResponse<GetGPSDevices.Response>> result = await sender.Send(new GetGPSDevices.Query(
            pageNumber!.Value,
            pageSize!.Value,
            keyword!
        ));
        return result.MapResult();
    }
}
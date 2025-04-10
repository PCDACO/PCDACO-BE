using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using UseCases.UC_GPSDevice.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.GPSDeviceEndpoints;

public class CheckIsDeviceCreatedEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/gps-devices/check-exist", Handle)
            .WithSummary("Check if GPS Device exists")
            .WithTags("GPS Devices")
            .WithDescription(
                "Checks if a GPS device with the specified OS Build ID already exists in the system"
            )
            .WithOpenApi(operation =>
            {
                operation.Responses["200"] = new()
                {
                    Description = "Success - Retrieved device existence status",
                    Content =
                    {
                        ["application/json"] = new()
                        {
                            Example = new OpenApiObject
                            {
                                ["value"] = new OpenApiObject
                                {
                                    ["isCreated"] = new OpenApiBoolean(true),
                                },
                                ["isSuccess"] = new OpenApiBoolean(true),
                                ["message"] = new OpenApiString("Lấy dữ liệu thành công"),
                            },
                        },
                    },
                };

                operation.Responses["400"] = new()
                {
                    Description = "Bad Request - Missing or invalid OSBuildId",
                    Content =
                    {
                        ["application/json"] = new()
                        {
                            Example = new OpenApiObject
                            {
                                ["isSuccess"] = new OpenApiBoolean(false),
                                ["message"] = new OpenApiString(
                                    "ID bản dựng hệ điều hành không hợp lệ"
                                ),
                            },
                        },
                    },
                };

                return operation;
            });
    }

    private async Task<IResult> Handle(
        ISender sender,
        [FromQuery(Name = "osBuildId")] string osBuildId
    )
    {
        Result<CheckIsDeviceCreated.Response> result = await sender.Send(
            new CheckIsDeviceCreated.Query(osBuildId)
        );
        return result.MapResult();
    }
}

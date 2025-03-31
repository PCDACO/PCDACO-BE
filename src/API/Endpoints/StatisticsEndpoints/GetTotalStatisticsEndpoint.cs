using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.OpenApi.Any;
using UseCases.UC_Statistics.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.StatisticsEndpoints;

public class GetTotalStatisticsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/statistics/total", Handle)
            .WithSummary("Get total system statistics")
            .WithTags("Statistics")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Retrieves comprehensive system-wide statistics for administrative dashboard.

                    Statistics include:
                    - Total revenue from all completed bookings
                    - Count of active users in the system
                    - Count of active transactions (pending or completed)
                    - Count of cars that have been rented at least once
                    - Count of cancelled bookings
                    - Total financial loss from cancelled bookings
                    - Monthly revenue data for the last 12 months
                    - Monthly active users data for the last 12 months

                    Notes:
                    - Only accessible by administrators
                    - Time-series data contains 12 months of statistics
                    - All monetary values are in VND
                    """,

                    Responses =
                    {
                        ["200"] = new()
                        {
                            Description = "Success - Returns total statistics data",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["data"] = new OpenApiObject
                                        {
                                            ["totalRevenue"] = new OpenApiDouble(1500000),
                                            ["activeUsers"] = new OpenApiInteger(250),
                                            ["activeTransactions"] = new OpenApiInteger(85),
                                            ["totalRentedCars"] = new OpenApiInteger(120),
                                            ["totalBookingCancelled"] = new OpenApiInteger(45),
                                            ["cancellationLoss"] = new OpenApiDouble(350000),
                                            ["revenueOverTime"] = new OpenApiArray
                                            {
                                                new OpenApiObject
                                                {
                                                    ["month"] = new OpenApiString("Jan"),
                                                    ["value"] = new OpenApiDouble(120000),
                                                },
                                                new OpenApiObject
                                                {
                                                    ["month"] = new OpenApiString("Feb"),
                                                    ["value"] = new OpenApiDouble(135000),
                                                },
                                                // Other months would follow the same pattern
                                            },
                                            ["activeUsersOverTime"] = new OpenApiArray
                                            {
                                                new OpenApiObject
                                                {
                                                    ["month"] = new OpenApiString("Jan"),
                                                    ["value"] = new OpenApiInteger(85),
                                                },
                                                new OpenApiObject
                                                {
                                                    ["month"] = new OpenApiString("Feb"),
                                                    ["value"] = new OpenApiInteger(92),
                                                },
                                                new OpenApiObject
                                                {
                                                    ["month"] = new OpenApiString("Mar"),
                                                    ["value"] = new OpenApiInteger(100),
                                                },
                                                new OpenApiObject
                                                {
                                                    ["month"] = new OpenApiString("Apr"),
                                                    ["value"] = new OpenApiInteger(110),
                                                },
                                                new OpenApiObject
                                                {
                                                    ["month"] = new OpenApiString("May"),
                                                    ["value"] = new OpenApiInteger(120),
                                                },
                                                new OpenApiObject
                                                {
                                                    ["month"] = new OpenApiString("Jun"),
                                                    ["value"] = new OpenApiInteger(130),
                                                },
                                                new OpenApiObject
                                                {
                                                    ["month"] = new OpenApiString("Jul"),
                                                    ["value"] = new OpenApiInteger(140),
                                                },
                                                new OpenApiObject
                                                {
                                                    ["month"] = new OpenApiString("Aug"),
                                                    ["value"] = new OpenApiInteger(150),
                                                },
                                                new OpenApiObject
                                                {
                                                    ["month"] = new OpenApiString("Sep"),
                                                    ["value"] = new OpenApiInteger(160),
                                                },
                                                new OpenApiObject
                                                {
                                                    ["month"] = new OpenApiString("Oct"),
                                                    ["value"] = new OpenApiInteger(170),
                                                },
                                                new OpenApiObject
                                                {
                                                    ["month"] = new OpenApiString("Nov"),
                                                    ["value"] = new OpenApiInteger(180),
                                                },
                                                new OpenApiObject
                                                {
                                                    ["month"] = new OpenApiString("Dec"),
                                                    ["value"] = new OpenApiInteger(190),
                                                },
                                            },
                                        },
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString(
                                            "Lấy thống kê hệ thống thành công"
                                        ),
                                    },
                                },
                            },
                        },
                        ["401"] = new()
                        {
                            Description = "Unauthorized - User not authenticated",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString("Bạn chưa đăng nhập"),
                                    },
                                },
                            },
                        },
                        ["403"] = new()
                        {
                            Description =
                                "Forbidden - User not authorized to view system statistics",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Bạn không có quyền truy cập thống kê hệ thống"
                                        ),
                                    },
                                },
                            },
                        },
                        ["500"] = new()
                        {
                            Description =
                                "Internal Server Error - Error while calculating statistics",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Có lỗi xảy ra khi tính toán thống kê"
                                        ),
                                    },
                                },
                            },
                        },
                    },
                }
            );
    }

    private async Task<IResult> Handle(ISender sender)
    {
        Result<GetTotalStatistics.Response> result = await sender.Send(
            new GetTotalStatistics.Query()
        );
        return result.MapResult();
    }
}

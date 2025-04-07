using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using UseCases.UC_Feedback.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.FeedbackEndpoints;

public class GetAllFeedbacksByCarIdEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/cars/{id:guid}/feedbacks", Handle)
            .WithSummary("Get all feedbacks for a specific car with statistics")
            .WithTags("Feedbacks")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Retrieve all feedbacks and statistics for a specific car.

                    Response includes:
                    - Feedback Statistics:
                      - Total number of feedbacks
                      - Average rating (1-5 scale, rounded to 1 decimal)
                      - Rating distribution (count for each rating level)

                    - Paginated Feedback List:
                      - Individual feedback details
                      - Sorted by most recent first
                      - Configurable page size

                    Notes:
                    - Only includes feedbacks of type 'ToOwner'
                    - Requires authentication
                    - Supports pagination
                    """,

                    Responses =
                    {
                        ["200"] = new()
                        {
                            Description =
                                "Success - Returns feedback statistics and paginated list",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["value"] = new OpenApiObject
                                        {
                                            ["stats"] = new OpenApiObject
                                            {
                                                ["totalFeedbacks"] = new OpenApiInteger(25),
                                                ["averageRating"] = new OpenApiDouble(4.2),
                                                ["ratingDistribution"] = new OpenApiObject
                                                {
                                                    ["1"] = new OpenApiInteger(1),
                                                    ["2"] = new OpenApiInteger(2),
                                                    ["3"] = new OpenApiInteger(3),
                                                    ["4"] = new OpenApiInteger(8),
                                                    ["5"] = new OpenApiInteger(11)
                                                }
                                            },
                                            ["feedbacks"] = new OpenApiObject
                                            {
                                                ["items"] = new OpenApiArray
                                                {
                                                    new OpenApiObject
                                                    {
                                                        ["id"] = new OpenApiString(
                                                            "123e4567-e89b-12d3-a456-426614174000"
                                                        ),
                                                        ["rating"] = new OpenApiInteger(5),
                                                        ["content"] = new OpenApiString(
                                                            "Xe rất tốt, chủ xe thân thiện"
                                                        ),
                                                        ["fromUserName"] = new OpenApiString(
                                                            "Nguyễn Văn A"
                                                        ),
                                                        ["toUserName"] = new OpenApiString(
                                                            "Trần Văn B"
                                                        ),
                                                        ["type"] = new OpenApiString("ToOwner"),
                                                        ["createdAt"] = new OpenApiString(
                                                            "2024-03-15T10:30:00Z"
                                                        )
                                                    }
                                                },
                                                ["totalCount"] = new OpenApiInteger(25),
                                                ["pageNumber"] = new OpenApiInteger(1),
                                                ["pageSize"] = new OpenApiInteger(10),
                                                ["hasNext"] = new OpenApiBoolean(true)
                                            }
                                        },
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString("Lấy dữ liệu thành công")
                                    }
                                }
                            }
                        },
                        ["401"] = new() { Description = "Unauthorized - User not authenticated" },
                        ["404"] = new()
                        {
                            Description = "Not Found - Car not found",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString("Không tìm thấy xe")
                                    }
                                }
                            }
                        }
                    },

                    Parameters =
                    {
                        new()
                        {
                            Name = "id",
                            In = ParameterLocation.Path,
                            Required = true,
                            Description = "The unique identifier of the car",
                            Schema = new() { Type = "string", Format = "uuid" }
                        },
                        new()
                        {
                            Name = "index",
                            In = ParameterLocation.Query,
                            Required = false,
                            Description = "Page number (1-based indexing)",
                            Schema = new()
                            {
                                Type = "integer",
                                Default = new OpenApiInteger(1),
                                Minimum = 1
                            }
                        },
                        new()
                        {
                            Name = "size",
                            In = ParameterLocation.Query,
                            Required = false,
                            Description = "Number of items per page",
                            Schema = new()
                            {
                                Type = "integer",
                                Default = new OpenApiInteger(10),
                                Minimum = 1,
                                Maximum = 50
                            }
                        }
                    }
                }
            );
    }

    private async Task<IResult> Handle(
        ISender sender,
        Guid id,
        [FromQuery(Name = "index")] int? pageNumber = 1,
        [FromQuery(Name = "size")] int? pageSize = 10
    )
    {
        Result<GetAllFeedbacksByCarId.Response> result = await sender.Send(
            new GetAllFeedbacksByCarId.Query(
                CarId: id,
                PageNumber: pageNumber!.Value,
                PageSize: pageSize!.Value
            )
        );

        return result.MapResult();
    }
}

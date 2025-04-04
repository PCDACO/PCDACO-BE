using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using UseCases.UC_Activities.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.ActivityEndpoints;

public class GetConsultantRecentActivityEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/users/consultant/recent-activity", Handle)
            .WithSummary("Get consultant's recent activities")
            .WithTags("Users")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Retrieves the 10 most recent activities for a consultant.

                    Activity Types:
                    1. Inspection Schedules:
                       - Created inspection schedules
                       - Shows status updates (Pending, Approved, Rejected, etc.)
                       - Includes car and technician details

                    2. Booking Reports:
                       - Reports resolved by the consultant
                       - Shows report status (Pending, UnderReview, Resolved, Rejected)
                       - Includes car and booking details

                    Response Format:
                    - Activities are sorted by timestamp (newest first)
                    - Maximum 10 activities returned
                    - Each activity includes:
                      * Avatar URL
                      * Content description (in Vietnamese)
                      * Timestamp
                      * Activity type (inspection/report)

                    Note: All car license plates in the response are automatically decrypted
                    """,

                    Responses = new()
                    {
                        ["200"] = new()
                        {
                            Description = "Success - Activities retrieved successfully",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["value"] = new OpenApiObject
                                        {
                                            ["activities"] = new OpenApiArray
                                            {
                                                new OpenApiObject
                                                {
                                                    ["avatarUrl"] = new OpenApiString(
                                                        "https://example.com/avatar.jpg"
                                                    ),
                                                    ["content"] = new OpenApiString(
                                                        "Lịch kiểm định xe Toyota Camry-51F12345 của Nguyễn Văn A đã được hoàn tất bởi kĩ thuật viên Trần Văn B"
                                                    ),
                                                    ["happenedAt"] = new OpenApiString(
                                                        "2024-03-15T10:30:00Z"
                                                    ),
                                                    ["type"] = new OpenApiString("inspection")
                                                },
                                                new OpenApiObject
                                                {
                                                    ["avatarUrl"] = new OpenApiString(
                                                        "https://example.com/avatar2.jpg"
                                                    ),
                                                    ["content"] = new OpenApiString(
                                                        "Báo cáo 'Xe bị hỏng' cho xe Honda Civic-51G12345 đã được giải quyết"
                                                    ),
                                                    ["happenedAt"] = new OpenApiString(
                                                        "2024-03-15T09:30:00Z"
                                                    ),
                                                    ["type"] = new OpenApiString("report")
                                                }
                                            }
                                        },
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString("Lấy dữ liệu thành công")
                                    }
                                }
                            }
                        },
                        ["401"] = new() { Description = "Unauthorized - User not authenticated" },
                        ["403"] = new()
                        {
                            Description = "Forbidden - User is not a consultant",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Bạn không có quyền truy cập tài nguyên này"
                                        )
                                    }
                                }
                            }
                        }
                    }
                }
            );
    }

    private async Task<IResult> Handle(ISender sender, CancellationToken cancellationToken)
    {
        Result<GetConsultantRecentActivity.Response> result = await sender.Send(
            new GetConsultantRecentActivity.Query(),
            cancellationToken
        );
        return result.MapResult();
    }
}

using API.Utils;
using Ardalis.Result;
using Carter;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using UseCases.DTOs;
using UseCases.UC_Contract.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.ContractEndpoints;

public class GetAllCarContractEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/cars/contracts", Handle)
            .WithSummary("Get all car contracts")
            .WithTags("Contracts")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Retrieve a paginated list of all car contracts in the system.

                    Access Control:
                    - Requires authentication
                    - Available to Admin, Consultant, and Technician roles
                    - Other roles will receive a 403 Forbidden response

                    Features:
                    - Pagination with configurable page size and number
                    - Filter by contract status
                    - Search by car license plate or owner name using keyword parameter
                    - Includes decrypted sensitive information for authorized users

                    Details Included:
                    - Contract ID and status
                    - Car information (model, license plate)
                    - Owner information
                    - Technician details (if assigned)
                    - Signature dates
                    - Inspection results and GPS device information
                    """,

                    Responses =
                    {
                        ["200"] = new()
                        {
                            Description = "Success - Returns paginated list of car contracts",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["value"] = new OpenApiObject
                                        {
                                            ["items"] = new OpenApiArray
                                            {
                                                new OpenApiObject
                                                {
                                                    ["id"] = new OpenApiString(
                                                        "123e4567-e89b-12d3-a456-426614174000"
                                                    ),
                                                    ["terms"] = new OpenApiString(
                                                        "Standard inspection terms and conditions"
                                                    ),
                                                    ["carId"] = new OpenApiString(
                                                        "123e4567-e89b-12d3-a456-426614174001"
                                                    ),
                                                    ["carModel"] = new OpenApiString(
                                                        "Toyota Camry"
                                                    ),
                                                    ["licensePlate"] = new OpenApiString(
                                                        "51G-123.45"
                                                    ),
                                                    ["ownerId"] = new OpenApiString(
                                                        "123e4567-e89b-12d3-a456-426614174002"
                                                    ),
                                                    ["ownerName"] = new OpenApiString("John Smith"),
                                                    ["technicianId"] = new OpenApiString(
                                                        "123e4567-e89b-12d3-a456-426614174003"
                                                    ),
                                                    ["technicianName"] = new OpenApiString(
                                                        "Alex Johnson"
                                                    ),
                                                    ["status"] = new OpenApiString("Signed"),
                                                    ["ownerSignatureDate"] = new OpenApiString(
                                                        "2024-03-15T10:00:00Z"
                                                    ),
                                                    ["technicianSignatureDate"] = new OpenApiString(
                                                        "2024-03-15T11:00:00Z"
                                                    ),
                                                    ["inspectionResults"] = new OpenApiString(
                                                        "Car passed inspection"
                                                    ),
                                                    ["gpsDeviceId"] = new OpenApiString(
                                                        "123e4567-e89b-12d3-a456-426614174004"
                                                    ),
                                                    ["createdAt"] = new OpenApiString(
                                                        "2024-03-14T09:00:00Z"
                                                    ),
                                                },
                                            },
                                            ["pageNumber"] = new OpenApiInteger(1),
                                            ["pageSize"] = new OpenApiInteger(10),
                                            ["totalItems"] = new OpenApiInteger(25),
                                            ["totalPages"] = new OpenApiInteger(3),
                                        },
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString("Lấy dữ liệu thành công"),
                                    },
                                },
                            },
                        },
                        ["401"] = new() { Description = "Unauthorized - User not authenticated" },
                        ["403"] = new()
                        {
                            Description = "Forbidden - User not authorized to view contract list",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Không có quyền truy cập danh sách hợp đồng"
                                        ),
                                    },
                                },
                            },
                        },
                    },
                }
            );
    }

    private async Task<IResult> Handle(
        ISender sender,
        [FromQuery(Name = "status")] CarContractStatusEnum? status,
        [FromQuery(Name = "pageNumber")] int pageNumber = 1,
        [FromQuery(Name = "pageSize")] int pageSize = 10,
        [FromQuery(Name = "keyword")] string? keyword = ""
    )
    {
        Result<OffsetPaginatedResponse<GetAllCarContracts.Response>> result = await sender.Send(
            new GetAllCarContracts.Query(pageNumber, pageSize, keyword ?? "", status)
        );
        return result.MapResult();
    }
}

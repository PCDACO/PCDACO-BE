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

public class GetAllBookingContractEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/bookings/contracts", Handle)
            .WithSummary("Get all booking contracts")
            .WithTags("Contracts")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Retrieve a paginated list of all booking contracts in the system.

                    Access Control:
                    - Admin and Consultant: Full access to all booking contracts
                    - Drivers: Can only see contracts for their own bookings
                    - Car Owners: Can only see contracts for bookings of their cars
                    - Other roles will receive a 403 Forbidden response

                    Features:
                    - Pagination with configurable page size and number
                    - Filter by contract status
                    - Search by license plate, driver name, or owner name using keyword parameter
                    - Results ordered by creation date (newest first)

                    Details Included:
                    - Contract ID, terms, and status
                    - Related booking information
                    - Car details (model, license plate)
                    - Owner and driver information
                    - Contract dates (start, end, signature dates)
                    """,

                    Responses =
                    {
                        ["200"] = new()
                        {
                            Description = "Success - Returns paginated list of booking contracts",
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
                                                        "Standard booking terms and conditions"
                                                    ),
                                                    ["bookingId"] = new OpenApiString(
                                                        "123e4567-e89b-12d3-a456-426614174001"
                                                    ),
                                                    ["carId"] = new OpenApiString(
                                                        "123e4567-e89b-12d3-a456-426614174002"
                                                    ),
                                                    ["carModel"] = new OpenApiString(
                                                        "Toyota Camry"
                                                    ),
                                                    ["licensePlate"] = new OpenApiString(
                                                        "51G-123.45"
                                                    ),
                                                    ["ownerId"] = new OpenApiString(
                                                        "123e4567-e89b-12d3-a456-426614174003"
                                                    ),
                                                    ["ownerName"] = new OpenApiString("John Smith"),
                                                    ["driverId"] = new OpenApiString(
                                                        "123e4567-e89b-12d3-a456-426614174004"
                                                    ),
                                                    ["driverName"] = new OpenApiString("Jane Doe"),
                                                    ["status"] = new OpenApiString("Pending"),
                                                    ["startDate"] = new OpenApiString(
                                                        "2024-03-15T10:00:00Z"
                                                    ),
                                                    ["endDate"] = new OpenApiString(
                                                        "2024-03-20T10:00:00Z"
                                                    ),
                                                    ["driverSignatureDate"] = new OpenApiString(
                                                        "2024-03-15T10:30:00Z"
                                                    ),
                                                    ["ownerSignatureDate"] = new OpenApiString(
                                                        "2024-03-15T11:00:00Z"
                                                    ),
                                                    ["createdAt"] = new OpenApiString(
                                                        "2024-03-14T09:00:00Z"
                                                    ),
                                                },
                                            },
                                            ["pageNumber"] = new OpenApiInteger(1),
                                            ["pageSize"] = new OpenApiInteger(10),
                                            ["totalItems"] = new OpenApiInteger(25),
                                            ["hasNext"] = new OpenApiBoolean(true),
                                        },
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString(
                                            "Contracts fetched successfully"
                                        ),
                                    },
                                },
                            },
                        },
                        ["400"] = new()
                        {
                            Description = "Bad Request - Invalid parameters",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Invalid contract status provided"
                                        ),
                                    },
                                },
                            },
                        },
                        ["401"] = new() { Description = "Unauthorized - User not authenticated" },
                        ["403"] = new()
                        {
                            Description = "Forbidden - User not authorized to view these contracts",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Bạn không có quyền xem các hợp đồng này"
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
        [FromQuery(Name = "status")] ContractStatusEnum? status,
        [FromQuery(Name = "pageNumber")] int pageNumber = 1,
        [FromQuery(Name = "pageSize")] int pageSize = 10,
        [FromQuery(Name = "keyword")] string? keyword = ""
    )
    {
        Result<OffsetPaginatedResponse<GetAllBookingContracts.Response>> result = await sender.Send(
            new GetAllBookingContracts.Query(pageNumber, pageSize, keyword ?? "", status)
        );
        return result.MapResult();
    }
}

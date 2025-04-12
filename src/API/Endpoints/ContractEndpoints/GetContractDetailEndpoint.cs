using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.OpenApi.Any;
using UseCases.UC_Contract.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.ContractEndpoints;

public class GetContractDetailEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/cars/contracts/{id:guid}", Handle)
            .WithSummary("Get detailed contract information by ID")
            .WithTags("Contracts")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Retrieve comprehensive details about a specific car contract.

                    Access Control:
                    - Requires authentication
                    - Restricted to Admin, Consultant, or Technician users
                    - Car owners can only view their own contracts

                    Details Included:
                    - Contract basic information (ID, status, signature dates)
                    - Car details (model, license plate, specifications, images, amenities)
                    - Owner information with contact details
                    - Technician information (if assigned)
                    - Inspection results and GPS device information
                    - Car location details (if GPS is available)

                    Notes:
                    - Sensitive information like phone numbers is automatically decrypted
                    - Created timestamp is derived from the contract's UUID
                    """,

                    Responses =
                    {
                        ["200"] = new()
                        {
                            Description = "Success",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["value"] = new OpenApiObject
                                        {
                                            ["id"] = new OpenApiString(
                                                "123e4567-e89b-12d3-a456-426614174000"
                                            ),
                                            ["status"] = new OpenApiString("Active"),
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
                                                "123e4567-e89b-12d3-a456-426614174001"
                                            ),
                                            ["createdAt"] = new OpenApiString(
                                                "2024-03-10T08:30:00Z"
                                            ),
                                            ["car"] = new OpenApiObject
                                            {
                                                ["id"] = new OpenApiString(
                                                    "123e4567-e89b-12d3-a456-426614174002"
                                                ),
                                                ["modelName"] = new OpenApiString("Toyota Camry"),
                                                ["licensePlate"] = new OpenApiString("51G-123.45"),
                                                ["color"] = new OpenApiString("Black"),
                                                ["seat"] = new OpenApiInteger(4),
                                                ["description"] = new OpenApiString(
                                                    "Well-maintained family sedan"
                                                ),
                                                ["terms"] = new OpenApiString(
                                                    "Standard rental terms"
                                                ),
                                                ["status"] = new OpenApiString("Available"),
                                                ["transmissionType"] = new OpenApiString(
                                                    "Automatic"
                                                ),
                                                ["fuelType"] = new OpenApiString("Gasoline"),
                                                ["price"] = new OpenApiFloat(500000),
                                                ["requiresCollateral"] = new OpenApiBoolean(true),
                                                ["fuelConsumption"] = new OpenApiFloat(7.5f),
                                                ["imageCarDetail"] = new OpenApiArray
                                                {
                                                    new OpenApiObject
                                                    {
                                                        ["id"] = new OpenApiString(
                                                            "123e4567-e89b-12d3-a456-426614174006"
                                                        ),
                                                        ["url"] = new OpenApiString(
                                                            "https://example.com/car1.jpg"
                                                        ),
                                                        ["type"] = new OpenApiString("Primary"),
                                                        ["name"] = new OpenApiString("Front View"),
                                                    },
                                                },
                                                ["manufacturerDetail"] = new OpenApiObject
                                                {
                                                    ["id"] = new OpenApiString(
                                                        "123e4567-e89b-12d3-a456-426614174007"
                                                    ),
                                                    ["name"] = new OpenApiString("Toyota"),
                                                },
                                                ["location"] = new OpenApiObject
                                                {
                                                    ["longtitude"] = new OpenApiFloat(106.6297f),
                                                    ["latitude"] = new OpenApiFloat(10.8231f),
                                                },
                                                ["amenities"] = new OpenApiArray
                                                {
                                                    new OpenApiObject
                                                    {
                                                        ["id"] = new OpenApiString(
                                                            "123e4567-e89b-12d3-a456-426614174008"
                                                        ),
                                                        ["name"] = new OpenApiString(
                                                            "GPS Navigation"
                                                        ),
                                                        ["description"] = new OpenApiString(
                                                            "Built-in GPS for navigation"
                                                        ),
                                                        ["iconUrl"] = new OpenApiString(
                                                            "https://example.com/icons/gps.png"
                                                        ),
                                                    },
                                                },
                                            },
                                            ["owner"] = new OpenApiObject
                                            {
                                                ["id"] = new OpenApiString(
                                                    "123e4567-e89b-12d3-a456-426614174003"
                                                ),
                                                ["name"] = new OpenApiString("John Smith"),
                                                ["email"] = new OpenApiString("john@example.com"),
                                                ["phone"] = new OpenApiString("0987654321"),
                                                ["address"] = new OpenApiString("123 Main St"),
                                                ["avatarUrl"] = new OpenApiString(
                                                    "https://example.com/avatar.jpg"
                                                ),
                                            },
                                            ["technician"] = new OpenApiObject
                                            {
                                                ["id"] = new OpenApiString(
                                                    "123e4567-e89b-12d3-a456-426614174004"
                                                ),
                                                ["name"] = new OpenApiString("Jane Tech"),
                                                ["email"] = new OpenApiString("jane@example.com"),
                                                ["phone"] = new OpenApiString("0123456789"),
                                            },
                                        },
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString(""),
                                    },
                                },
                            },
                        },
                        ["401"] = new() { Description = "Unauthorized - User not authenticated" },
                        ["403"] = new()
                        {
                            Description = "Forbidden - User does not have access to this contract",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Bạn không có quyền xem hợp đồng này"
                                        ),
                                    },
                                },
                            },
                        },
                        ["404"] = new()
                        {
                            Description = "Not Found - Contract does not exist",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString("Không tìm thấy hợp đồng"),
                                    },
                                },
                            },
                        },
                    },
                }
            );
    }

    private async Task<IResult> Handle(ISender sender, Guid id)
    {
        Result<GetContractDetail.Response> result = await sender.Send(
            new GetContractDetail.Query(id)
        );
        return result.MapResult();
    }
}

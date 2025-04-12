using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.OpenApi.Any;
using UseCases.UC_Contract.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.ContractEndpoints;

public class GetBookingContractByIdEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/bookings/contracts/{id:guid}", Handle)
            .WithSummary("Get booking contract by ID")
            .WithTags("Contracts")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Retrieve detailed information about a specific booking contract.

                    Access Control:
                    - Only Admin users can view contract details

                    Details Included:
                    - Contract metadata (ID, status, terms, dates)
                    - Car details (model, license plate, color, seat count, description)
                    - Car location coordinates (if GPS is available)
                    - Car manufacturer information
                    - Car images with type and name
                    - Car amenities with description and icon URL
                    - Owner information with contact details
                    - Driver information with contact details
                    - Booking details and payment information
                    - Electronic signature status (dates when parties signed)

                    Notes:
                    - Sensitive information like phone numbers is automatically decrypted
                    - Created timestamp is derived from the contract's UUID
                    """,

                    Responses =
                    {
                        ["200"] = new()
                        {
                            Description = "Success - Returns detailed contract information",
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
                                            ["terms"] = new OpenApiString(
                                                "Standard rental terms and conditions"
                                            ),
                                            ["bookingId"] = new OpenApiString(
                                                "123e4567-e89b-12d3-a456-426614174001"
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
                                                ["location"] = new OpenApiObject
                                                {
                                                    ["longtitude"] = new OpenApiDouble(106.6297),
                                                    ["latitude"] = new OpenApiDouble(10.8231),
                                                },
                                                ["manufacturer"] = new OpenApiObject
                                                {
                                                    ["id"] = new OpenApiString(
                                                        "123e4567-e89b-12d3-a456-426614174005"
                                                    ),
                                                    ["name"] = new OpenApiString("Toyota"),
                                                },
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
                                                        ["type"] = new OpenApiString("Exterior"),
                                                        ["name"] = new OpenApiString("Front view"),
                                                    },
                                                    new OpenApiObject
                                                    {
                                                        ["id"] = new OpenApiString(
                                                            "123e4567-e89b-12d3-a456-426614174007"
                                                        ),
                                                        ["url"] = new OpenApiString(
                                                            "https://example.com/car2.jpg"
                                                        ),
                                                        ["type"] = new OpenApiString("Interior"),
                                                        ["name"] = new OpenApiString("Dashboard"),
                                                    },
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
                                                            "Built-in GPS navigation system"
                                                        ),
                                                        ["iconUrl"] = new OpenApiString(
                                                            "https://example.com/icons/gps.png"
                                                        ),
                                                    },
                                                    new OpenApiObject
                                                    {
                                                        ["id"] = new OpenApiString(
                                                            "123e4567-e89b-12d3-a456-426614174009"
                                                        ),
                                                        ["name"] = new OpenApiString("Bluetooth"),
                                                        ["description"] = new OpenApiString(
                                                            "Bluetooth connectivity for phone and audio"
                                                        ),
                                                        ["iconUrl"] = new OpenApiString(
                                                            "https://example.com/icons/bluetooth.png"
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
                                                ["address"] = new OpenApiString(
                                                    "456 Oak Avenue, District 2, HCMC"
                                                ),
                                                ["phone"] = new OpenApiString("0987654321"),
                                            },
                                            ["driver"] = new OpenApiObject
                                            {
                                                ["id"] = new OpenApiString(
                                                    "123e4567-e89b-12d3-a456-426614174004"
                                                ),
                                                ["name"] = new OpenApiString("Jane Doe"),
                                                ["email"] = new OpenApiString("jane@example.com"),
                                                ["address"] = new OpenApiString(
                                                    "789 Pine Road, District 3, HCMC"
                                                ),
                                                ["phone"] = new OpenApiString("0123456789"),
                                            },
                                            ["status"] = new OpenApiString("Confirmed"),
                                            ["startDate"] = new OpenApiString(
                                                "2024-03-15T10:00:00Z"
                                            ),
                                            ["endDate"] = new OpenApiString("2024-03-20T10:00:00Z"),
                                            ["driverSignatureDate"] = new OpenApiString(
                                                "2024-03-14T15:30:00Z"
                                            ),
                                            ["ownerSignatureDate"] = new OpenApiString(
                                                "2024-03-14T16:45:00Z"
                                            ),
                                            ["basePrice"] = new OpenApiDouble(2000000),
                                            ["totalAmount"] = new OpenApiDouble(2200000),
                                            ["createdAt"] = new OpenApiString(
                                                "2024-03-14T09:00:00Z"
                                            ),
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
                            Description = "Forbidden - User is not an admin",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Bạn không có quyền xem thông tin này"
                                        ),
                                    },
                                },
                            },
                        },
                        ["404"] = new()
                        {
                            Description = "Not Found - Contract doesn't exist",
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
        Result<GetBookingContractById.Response> result = await sender.Send(
            new GetBookingContractById.Query(id)
        );
        return result.MapResult();
    }
}

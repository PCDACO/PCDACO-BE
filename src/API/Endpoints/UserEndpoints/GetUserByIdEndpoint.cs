using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.OpenApi.Any;
using UseCases.UC_User.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.UserEndpoints;

public class GetUserByIdEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/users/{id:guid}", Handle)
            .WithSummary("Get user by ID")
            .WithTags("Users")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Retrieves comprehensive information about a specific user by their ID.

                    Features:
                    - Returns complete user profile information including personal details
                    - Includes user's cars with detailed specifications and location information
                    - Includes user's booking history with complete transaction details
                    - Includes reports associated with user's cars and their current status
                    - Decrypts sensitive information like phone numbers and license numbers
                    - Requires authentication to access

                    This endpoint is useful for user profiles, administrative reviews, and detailed 
                    user activity tracking purposes.
                    """,

                    Responses =
                    {
                        ["200"] = new()
                        {
                            Description =
                                "Success - Returns detailed user information with related collections",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["value"] = new OpenApiObject
                                        {
                                            // Basic user information
                                            ["id"] = new OpenApiString(
                                                "123e4567-e89b-12d3-a456-426614174000"
                                            ),
                                            ["encryptionKeyId"] = new OpenApiString(
                                                "123e4567-e89b-12d3-a456-426614174001"
                                            ),
                                            ["roleId"] = new OpenApiString(
                                                "123e4567-e89b-12d3-a456-426614174002"
                                            ),
                                            ["avatarUrl"] = new OpenApiString(
                                                "https://example.com/avatar.jpg"
                                            ),
                                            ["name"] = new OpenApiString("Nguyễn Văn A"),
                                            ["email"] = new OpenApiString("nguyenvana@example.com"),
                                            ["address"] = new OpenApiString(
                                                "123 Đường ABC, Quận 1, TP.HCM"
                                            ),
                                            ["dateOfBirth"] = new OpenApiString(
                                                "1990-01-01T00:00:00Z"
                                            ),
                                            ["phone"] = new OpenApiString("0987654321"),
                                            ["balance"] = new OpenApiFloat(1500000),
                                            ["lockedBalance"] = new OpenApiFloat(0),
                                            ["licenseNumber"] = new OpenApiString("29A-12345"),
                                            ["licenseImageFrontUrl"] = new OpenApiString(
                                                "https://example.com/license-front.jpg"
                                            ),
                                            ["licenseImageBackUrl"] = new OpenApiString(
                                                "https://example.com/license-back.jpg"
                                            ),
                                            ["licenseExpiryDate"] = new OpenApiString(
                                                "2025-01-01T00:00:00Z"
                                            ),
                                            ["licenseIsApproved"] = new OpenApiBoolean(true),
                                            ["licenseRejectReason"] = new OpenApiNull(),
                                            ["licenseImageUploadedAt"] = new OpenApiString(
                                                "2022-01-01T00:00:00Z"
                                            ),
                                            ["licenseApprovedAt"] = new OpenApiString(
                                                "2022-01-02T00:00:00Z"
                                            ),
                                            ["isBanned"] = new OpenApiBoolean(false),
                                            ["bannedReason"] = new OpenApiString(""),
                                            ["role"] = new OpenApiString("Driver"),

                                            // User's cars
                                            ["cars"] = new OpenApiArray
                                            {
                                                new OpenApiObject
                                                {
                                                    ["id"] = new OpenApiString(
                                                        "123e4567-e89b-12d3-a456-426614174010"
                                                    ),
                                                    ["ownerId"] = new OpenApiString(
                                                        "123e4567-e89b-12d3-a456-426614174000"
                                                    ),
                                                    ["modelId"] = new OpenApiString(
                                                        "123e4567-e89b-12d3-a456-426614174011"
                                                    ),
                                                    ["encryptionKeyId"] = new OpenApiString(
                                                        "123e4567-e89b-12d3-a456-426614174012"
                                                    ),
                                                    ["fuelTypeId"] = new OpenApiString(
                                                        "123e4567-e89b-12d3-a456-426614174013"
                                                    ),
                                                    ["transmissionTypeId"] = new OpenApiString(
                                                        "123e4567-e89b-12d3-a456-426614174014"
                                                    ),
                                                    ["status"] = new OpenApiString("Active"),
                                                    ["licensePlate"] = new OpenApiString(
                                                        "51F-12345"
                                                    ),
                                                    ["color"] = new OpenApiString("Đen"),
                                                    ["seat"] = new OpenApiInteger(5),
                                                    ["description"] = new OpenApiString(
                                                        "Xe gia đình, sạch sẽ"
                                                    ),
                                                    ["fuelConsumption"] = new OpenApiFloat(7.5f),
                                                    ["requiresCollateral"] = new OpenApiBoolean(
                                                        true
                                                    ),
                                                    ["price"] = new OpenApiFloat(800000),
                                                    ["terms"] = new OpenApiString(
                                                        "Không hút thuốc trong xe"
                                                    ),
                                                    ["pickupLocation"] = new OpenApiObject
                                                    {
                                                        ["longitude"] = new OpenApiDouble(106.6297),
                                                        ["latitude"] = new OpenApiDouble(10.8231),
                                                        ["address"] = new OpenApiString(
                                                            "456 Đường XYZ, Quận 2, TP.HCM"
                                                        ),
                                                    },
                                                    ["modelName"] = new OpenApiString("Camry"),
                                                    ["manufacturerName"] = new OpenApiString(
                                                        "Toyota"
                                                    ),
                                                    ["fuelTypeName"] = new OpenApiString("Xăng"),
                                                    ["transmissionTypeName"] = new OpenApiString(
                                                        "Tự động"
                                                    ),
                                                    ["imageUrls"] = new OpenApiArray
                                                    {
                                                        new OpenApiString(
                                                            "https://example.com/car1.jpg"
                                                        ),
                                                        new OpenApiString(
                                                            "https://example.com/car2.jpg"
                                                        ),
                                                    },
                                                },
                                            },

                                            // User's bookings
                                            ["bookings"] = new OpenApiArray
                                            {
                                                new OpenApiObject
                                                {
                                                    ["id"] = new OpenApiString(
                                                        "123e4567-e89b-12d3-a456-426614174020"
                                                    ),
                                                    ["userId"] = new OpenApiString(
                                                        "123e4567-e89b-12d3-a456-426614174000"
                                                    ),
                                                    ["carId"] = new OpenApiString(
                                                        "123e4567-e89b-12d3-a456-426614174021"
                                                    ),
                                                    ["status"] = new OpenApiString("Completed"),
                                                    ["startTime"] = new OpenApiString(
                                                        "2023-05-01T08:00:00Z"
                                                    ),
                                                    ["endTime"] = new OpenApiString(
                                                        "2023-05-03T18:00:00Z"
                                                    ),
                                                    ["actualReturnTime"] = new OpenApiString(
                                                        "2023-05-03T17:30:00Z"
                                                    ),
                                                    ["basePrice"] = new OpenApiFloat(1600000),
                                                    ["platformFee"] = new OpenApiFloat(160000),
                                                    ["excessDay"] = new OpenApiFloat(0),
                                                    ["excessDayFee"] = new OpenApiFloat(0),
                                                    ["totalAmount"] = new OpenApiFloat(1760000),
                                                    ["totalDistance"] = new OpenApiFloat(120),
                                                    ["note"] = new OpenApiString(
                                                        "Chuyến đi gia đình"
                                                    ),
                                                    ["isCarReturned"] = new OpenApiBoolean(true),
                                                    ["payOSOrderCode"] = new OpenApiLong(
                                                        1234567890
                                                    ),
                                                    ["isPaid"] = new OpenApiBoolean(true),
                                                    ["isRefund"] = new OpenApiBoolean(false),
                                                    ["refundAmount"] = new OpenApiNull(),
                                                    ["refundDate"] = new OpenApiNull(),
                                                    ["carModelName"] = new OpenApiString("Innova"),
                                                    ["driverName"] = new OpenApiString(
                                                        "Nguyễn Văn A"
                                                    ),
                                                },
                                            },

                                            // Reports related to user's cars
                                            ["reports"] = new OpenApiArray
                                            {
                                                new OpenApiObject
                                                {
                                                    ["id"] = new OpenApiString(
                                                        "123e4567-e89b-12d3-a456-426614174030"
                                                    ),
                                                    ["bookingId"] = new OpenApiString(
                                                        "123e4567-e89b-12d3-a456-426614174031"
                                                    ),
                                                    ["reportedById"] = new OpenApiString(
                                                        "123e4567-e89b-12d3-a456-426614174032"
                                                    ),
                                                    ["title"] = new OpenApiString(
                                                        "Xe bị trầy xước"
                                                    ),
                                                    ["reportType"] = new OpenApiString("CarDamage"),
                                                    ["description"] = new OpenApiString(
                                                        "Xe bị trầy xước ở cánh cửa bên phải"
                                                    ),
                                                    ["status"] = new OpenApiString("UnderReview"),
                                                    ["compensationPaidUserId"] = new OpenApiString(
                                                        "123e4567-e89b-12d3-a456-426614174033"
                                                    ),
                                                    ["compensationReason"] = new OpenApiString(
                                                        "Bồi thường thiệt hại cho chủ xe"
                                                    ),
                                                    ["compensationAmount"] = new OpenApiFloat(
                                                        500000
                                                    ),
                                                    ["isCompensationPaid"] = new OpenApiBoolean(
                                                        false
                                                    ),
                                                    ["compensationPaidImageUrl"] =
                                                        new OpenApiNull(),
                                                    ["compensationPaidAt"] = new OpenApiNull(),
                                                    ["resolvedAt"] = new OpenApiString(
                                                        "2023-05-15T10:30:00Z"
                                                    ),
                                                    ["resolvedById"] = new OpenApiString(
                                                        "123e4567-e89b-12d3-a456-426614174034"
                                                    ),
                                                    ["resolutionComments"] = new OpenApiString(
                                                        "Đã xác nhận thiệt hại và yêu cầu bồi thường"
                                                    ),
                                                    ["reporterName"] = new OpenApiString(
                                                        "Trần Văn B"
                                                    ),
                                                    ["resolverName"] = new OpenApiString(
                                                        "Lê Thị C"
                                                    ),
                                                    ["imageUrls"] = new OpenApiArray
                                                    {
                                                        new OpenApiString(
                                                            "https://example.com/damage1.jpg"
                                                        ),
                                                        new OpenApiString(
                                                            "https://example.com/damage2.jpg"
                                                        ),
                                                    },
                                                },
                                            },
                                        },
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString("Lấy dữ liệu thành công"),
                                    },
                                },
                            },
                        },
                        ["401"] = new() { Description = "Unauthorized - User not authenticated" },
                        ["404"] = new()
                        {
                            Description = "Not Found - User doesn't exist",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Không tìm thấy người dùng"
                                        ),
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
        Result<GetUserById.Response> result = await sender.Send(new GetUserById.Query(id));
        return result.MapResult();
    }
}

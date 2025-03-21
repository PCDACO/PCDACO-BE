using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.OpenApi.Any;
using UseCases.UC_Car.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.CarEndpoints;

public class UpdateCarEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/cars/{id:guid}", Handle)
            .WithSummary("Update car details")
            .WithTags("Cars")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Update comprehensive details of a specific car.

                    Access Control:
                    - Requires authentication
                    - Only the car owner can update their car
                    - Not available to Admin users

                    Update Process:
                    - Validates all reference entities (model, transmission, fuel type)
                    - Updates amenities list completely
                    - Re-encrypts license plate
                    - Updates location information
                    - Updates all car specifications

                    Validation Rules:
                    - License plate: 8-11 characters
                    - Seats: 1-49
                    - Description: max 500 characters
                    - Fuel consumption: must be positive
                    - Price: must be positive
                    - All reference IDs must exist

                    Note: This is a complete update operation. All fields must be provided,
                    even if they haven't changed.
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
                                            )
                                        },
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString("Cập nhật thành công")
                                    }
                                }
                            }
                        },
                        ["400"] = new()
                        {
                            Description = "Bad Request - Validation errors",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString("Validation errors"),
                                        ["errors"] = new OpenApiArray
                                        {
                                            new OpenApiString("Biển số xe không được để trống !"),
                                            new OpenApiString("Số chỗ ngồi phải lớn hơn 0 !")
                                        }
                                    }
                                }
                            }
                        },
                        ["401"] = new() { Description = "Unauthorized - User not authenticated" },
                        ["403"] = new()
                        {
                            Description = "Forbidden - User is not the car owner or is an admin",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Bạn không có quyền thực hiện thao tác này"
                                        )
                                    }
                                }
                            }
                        },
                        ["404"] = new()
                        {
                            Description = "Not Found - Car or referenced entities don't exist",
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
                    }
                }
            );
    }

    private async Task<IResult> Handle(ISender sender, Guid id, UpdateCarRequest request)
    {
        Result<UpdateCar.Response> result = await sender.Send(
            new UpdateCar.Commamnd(
                CarId: id,
                AmenityIds: request.AmenityIds,
                ModelId: request.ModelId,
                TransmissionTypeId: request.TransmissionTypeId,
                FuelTypeId: request.FuelTypeId,
                LicensePlate: request.LicensePlate,
                Color: request.Color,
                Seat: request.Seat,
                Description: request.Description,
                Price: request.Price,
                FuelConsumption: request.FuelConsumption,
                RequiresCollateral: request.RequiresCollateral,
                PickupLatitude: request.PickupLatitude,
                PickupLongitude: request.PickupLongitude,
                PickupAddress: request.PickupAddress
            )
        );
        return result.MapResult();
    }

    private record UpdateCarRequest(
        Guid[] AmenityIds,
        Guid ModelId,
        Guid TransmissionTypeId,
        Guid FuelTypeId,
        string LicensePlate,
        string Color,
        int Seat,
        string Description,
        decimal FuelConsumption,
        bool RequiresCollateral,
        decimal Price,
        decimal PickupLatitude,
        decimal PickupLongitude,
        string PickupAddress
    );
}
